using System;
using System.ComponentModel.DataAnnotations;

namespace JobProcessor.Contracts;

public class CreatePatientRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }
}
