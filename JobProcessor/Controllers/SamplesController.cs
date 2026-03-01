using JobProcessor.Contracts;
using JobProcessor.Core;
using JobProcessor.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobProcessor.Controllers
{
    [Route("samples")]
    [ApiController]
    public class SamplesController : ControllerBase
    {
        private readonly JobDbContext _dbContext;
        private readonly IIdGenerator _idGenerator;

        public SamplesController(JobDbContext dbContext, IIdGenerator idGenerator)
        {
            _dbContext = dbContext;
            _idGenerator = idGenerator;
        }

        [HttpPost("/patients/{patientId:long}/samples")]
        public async Task<IActionResult> CreateSample(long patientId, [FromBody] CreateSampleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SampleType))
            {
                return BadRequest("SampleType is required.");
            }

            if (patientId <= 0)
            {
                return BadRequest("PatientId is required.");
            }

            var patientExists = await _dbContext.Patients
                .AnyAsync(p => p.PatientId == patientId);
            if (!patientExists)
            {
                return NotFound("Patient not found.");
            }

            var sample = new Sample
            {
                SampleId = _idGenerator.NextId(),
                SampleType = request.SampleType,
                PatientId = patientId,
                CollectedAt = DateTime.UtcNow
            };

            _dbContext.Samples.Add(sample);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetSample),
                new { sampleId = sample.SampleId },
                ToSampleResponse(sample));
        }

        [HttpGet("{sampleId:long}")]
        public async Task<IActionResult> GetSample(long sampleId)
        {
            var sample = await _dbContext.Samples.FindAsync(sampleId);
            if (sample == null)
            {
                return NotFound();
            }

            return Ok(ToSampleResponse(sample));
        }

        [HttpGet("/patients/{patientId:long}/samples")]
        public async Task<IActionResult> GetSamplesByPatient(long patientId)
        {
            if (patientId <= 0)
            {
                return BadRequest("PatientId is required.");
            }

            var patientExists = await _dbContext.Patients
                .AnyAsync(p => p.PatientId == patientId);
            if (!patientExists)
            {
                return NotFound("Patient not found.");
            }

            var samples = await _dbContext.Samples
                .Where(s => s.PatientId == patientId)
                .OrderByDescending(s => s.CollectedAt)
                .ToListAsync();

            return Ok(samples.Select(ToSampleResponse));
        }

        private static SampleResponse ToSampleResponse(Sample sample)
        {
            return new SampleResponse
            {
                SampleId = sample.SampleId.ToString(),
                SampleType = sample.SampleType,
                CollectedAt = sample.CollectedAt,
                PatientId = sample.PatientId.ToString()
            };
        }
    }
}
