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

        if (paciente.Bloqueado)
            return BadRequest(new { mensaje = "El paciente se encuentra bloqueado para agendar turnos online." });

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

        // Cambio: uso IsWithinCancellationWindow
        if (turno.FechaHora.IsWithinCancellationWindow())
            return BadRequest(new { mensaje = "No se puede cancelar con menos de 24 horas de anticipación." });

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

        if (!turno.FechaHora.IsWithinCancellationWindow())
            return BadRequest(new { mensaje = "La ausencia solo puede registrarse dentro de las 24 horas del turno." });

        turno.Estado = EstadoTurno.NoShow;
        await _context.SaveChangesAsync();
        return Ok(turno);
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        turno.Estado = request.Estado;
        await _context.SaveChangesAsync();
        return Ok(turno);
    }
}

public class ActualizarEstadoRequest
{
    public EstadoTurno Estado { get; set; }
}
