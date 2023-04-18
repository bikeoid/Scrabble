using System;
using System.Collections.Generic;
using FluentValidation;

namespace Scrabble.Shared;

public class PlayerDto
{


    public int PlayerId { get; set; }

    public string Email { get; set; }

    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsPlayer { get; set; }
    public bool EnableSound { get; set; }
    public bool WordCheck { get; set; }
    public bool NotifyNewMoveByEmail { get; set; }
}


public class PlayerDtoValidator : AbstractValidator<PlayerDto>
{
    public PlayerDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Length(1, 80).EmailAddress()
        .WithMessage("Please enter a valid Email");
        RuleFor(x => x.Name).NotEmpty().Length(1, 80)
        .WithMessage("Please enter a name");
    }
}

