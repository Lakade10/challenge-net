using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.Models;

namespace TurnosMedicos.Controllers;

[ApiController]
[Route("[controller]")]
public class PacientesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PacientesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Cambio: filtramos para traer solo a los pacientes activos
        var pacientes = await _context.Pacientes.Where(p => p.isActive).ToListAsync();
        return Ok(pacientes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null) return NotFound();
        return Ok(paciente);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Paciente paciente)
    {
        // Cambio: validamos si ya hay un paciente con el mismo DNI
        var existeDni = await _context.Pacientes.AnyAsync(p => p.DNI == paciente.DNI);
        if (existeDni)
            return BadRequest(new { mensaje = "Ya existe un paciente registrado con ese DNI." });

        paciente.createdAt = DateTime.Now;
        paciente.isActive = true;
        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, paciente);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Paciente paciente)
    {
        var existing = await _context.Pacientes.FindAsync(id);
        if (existing == null) return NotFound();

        existing.NombreCompleto = paciente.NombreCompleto;
        existing.DNI = paciente.DNI;
        existing.Email = paciente.Email;
        existing.Telefono = paciente.Telefono;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null) return NotFound();
        // Cambio: cambiamos a baja lógica para persistir la información del paciente en BD
        paciente.isActive = false;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
