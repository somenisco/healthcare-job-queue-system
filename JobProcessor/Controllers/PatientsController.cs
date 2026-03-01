using JobProcessor.Contracts;
using JobProcessor.Core;
using JobProcessor.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobProcessor.Controllers
{
    [Route("patients")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly JobDbContext _dbContext;
        private readonly IIdGenerator _idGenerator;

        public PatientsController(JobDbContext dbContext, IIdGenerator idGenerator)
        {
            _dbContext = dbContext;
            _idGenerator = idGenerator;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request)
        {
            var normalizedName = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName) ||
                string.Equals(normalizedName, "string", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(request.Name), "Name must be a real value, not empty or placeholder text.");
                return ValidationProblem(ModelState);
            }

            var patient = new Patient
            {
                PatientId = _idGenerator.NextId(),
                Name = normalizedName,
                DateOfBirth = request.DateOfBirth
            };

            _dbContext.Patients.Add(patient);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPatient), new { patientId = patient.PatientId }, ToResponse(patient));
        }

        [HttpGet("{patientId:long}")]
        public async Task<IActionResult> GetPatient(long patientId)
        {
            var patient = await _dbContext.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(patient));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPatients()
        {
            var patients = await _dbContext.Patients.ToListAsync();
            return Ok(patients.Select(ToResponse));
        }

        private static PatientResponse ToResponse(Patient patient)
        {
            return new PatientResponse
            {
                PatientId = patient.PatientId.ToString(),
                Name = patient.Name,
                DateOfBirth = patient.DateOfBirth
            };
        }
    }
}
