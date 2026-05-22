using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.Helpers;
using TurnosMedicos.Models;

namespace TurnosMedicos.Controllers;

[ApiController]
[Route("[controller]")]
public class TurnosController : ControllerBase
{
    private readonly AppDbContext _context;

    public TurnosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var turnos = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .Where(t => t.Paciente != null && t.Paciente.isActive) // Cambio: devolvemos turnos de pacientes activos
            .ToListAsync();
        return Ok(turnos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null) return NotFound();
        return Ok(turno);
    }

    [HttpPost]
    public async Task<IActionResult> CrearTurno([FromBody] Turno turno)
    {
        var paciente = await _context.Pacientes.FindAsync(turno.PacienteId);
        if (paciente == null)
            return NotFound(new { mensaje = "Paciente no encontrado." });

        // Cambio: chequeo si está bloqueado y si ya pasaron 30 días desde que se bloqueó al paciente para reiniciar su bloqueo. Sino, mostramos hasta cuando está bloqueado.
        if (paciente.Bloqueado)
        {
            if (paciente.FechaBloqueo.HasValue && DateTime.Now >= paciente.FechaBloqueo.Value.AddDays(30))
            {
                paciente.Bloqueado = false;
                paciente.NoShowCount = 0;
                paciente.FechaBloqueo = null;
            }
            else
            {
                var fechaDesbloqueo = paciente.FechaBloqueo.HasValue
                    ? paciente.FechaBloqueo.Value.AddDays(30).ToString("dd/MM/yyyy")
                    : "próximamente";
                return BadRequest(new { mensaje = $"El paciente se encuentra bloqueado por ausencias hasta el {fechaDesbloqueo}." });
            }
        }

        var medicoExiste = await _context.Medicos.AnyAsync(m => m.Id == turno.MedicoId);
        if (!medicoExiste)
            return NotFound(new { mensaje = "Médico no encontrado." });

        var turnoConflicto = await _context.Turnos.AnyAsync(t =>
            t.MedicoId == turno.MedicoId &&
            t.FechaHora == turno.FechaHora &&
            t.Estado != EstadoTurno.Cancelado);
        if (turnoConflicto)
            return BadRequest(new { mensaje = "El médico ya tiene un turno en ese horario." });
        // Cambio: validación de longitud de Motivo (podría ser un data anottation en el modelo de Turno)
        if (turno.Motivo.Length > 100)
            return BadRequest(new { mensaje = "El motivo no puede superar los 100 caracteres." });
        // Cambio: validación de FechaHora posterior a la actual
        if (turno.FechaHora <= DateTime.Now)
            return BadRequest(new { mensaje = "El turno no puede ser creado para la fecha y hora anterior o igual a la actual." });
        // Cambio: UtcNow por Now
        turno.FechaCreacion = DateTime.Now;
        turno.Estado = EstadoTurno.Pendiente;
        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = turno.Id }, turno);
    }

    // Cambio: HttpGet a HttpPut y lo deje como "{id}/cancelar" para ordenar lógicamente las acciones
    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> CancelarTurno(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        // Cambio: si el turno está cancelado o ausentado, evitamos otro cambio de estado para no sumar un NoShow
        if (turno.Estado == EstadoTurno.Cancelado || turno.Estado == EstadoTurno.NoShow)
            return BadRequest(new { mensaje = "El turno ya se encuentra cancelado o marcado como ausencia." });

        // Cambio: uso IsWithinCancellationWindow y sumamos NoShowCount solo si se cancela dentro de las 24 horas del turno
        if (turno.FechaHora.IsWithinCancellationWindow())
        {
            var paciente = await _context.Pacientes.FindAsync(turno.PacienteId);
            if (paciente != null)
            {
                ProcesarBloqueo(paciente);
            }
        }

        turno.Estado = EstadoTurno.Cancelado;
        await _context.SaveChangesAsync();
        return Ok(turno);
    }

    // Cambio: HttpPost a HttpPut y lo deje como "{id}/ausentar" para ordenar lógicamente las acciones
    [HttpPut("{id}/ausentar")]
    public async Task<IActionResult> MarcarAusencia(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        // Cambio: si el turno está cancelado o ausentado, evitamos otro cambio de estado para no sumar un NoShow
        if (turno.Estado == EstadoTurno.Cancelado || turno.Estado == EstadoTurno.NoShow)
            return BadRequest(new { mensaje = "El turno ya se encuentra cancelado o marcado como ausencia." });

        if (!turno.FechaHora.IsWithinCancellationWindow())
            return BadRequest(new { mensaje = "La ausencia solo puede registrarse dentro de las 24 horas del turno." });

        // Cambio: si ausentamos, sumamos NoShow
        var paciente = await _context.Pacientes.FindAsync(turno.PacienteId);
        if (paciente != null)
        {
            ProcesarBloqueo(paciente);
        }
        turno.Estado = EstadoTurno.NoShow;
        await _context.SaveChangesAsync();
        return Ok(turno);
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        // Cambio: si el turno está cancelado o ausentado, evitamos otro cambio de estado para no sumar un NoShow
        if (turno.Estado == EstadoTurno.Cancelado || turno.Estado == EstadoTurno.NoShow)
            return BadRequest(new { mensaje = "El turno ya se encuentra cancelado o marcado como ausencia." });

        turno.Estado = request.Estado;
        await _context.SaveChangesAsync();
        return Ok(turno);
    }

    // Cambio: función para sumar NoShow (ausencia) al paciente y bloquearlo si llega a 3
    private void ProcesarBloqueo(Paciente paciente)
    {
        paciente.NoShowCount++;

        if (paciente.NoShowCount >= 3)
        {
            paciente.Bloqueado = true;
            paciente.FechaBloqueo = DateTime.Now;
        }
    }

}

public class ActualizarEstadoRequest
{
    public EstadoTurno Estado { get; set; }
}
