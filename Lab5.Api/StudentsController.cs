using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab5.Api.Data;
using Lab5.Api.Models;

namespace Lab5.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase {
    private readonly AppDbContext _context;
    public StudentsController(AppDbContext context) => _context = context;
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents() 
    {
        return await _context.Students.ToListAsync();
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return NotFound();
        return student;
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Student>>> SearchStudents(string name)
    {
        var query = "SELECT * FROM \"Students\" WHERE \"FullName\" LIKE {0}";
        return await _context.Students
            .FromSqlRaw(query, $"%{name}%")
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Student>> PostStudent([FromBody] Student student) {
        try 
        {
            student.Id = 0;
            student.Enrollments = new List<Enrollment>();

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return StatusCode(201, student);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudent(int id) {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return NotFound();

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}