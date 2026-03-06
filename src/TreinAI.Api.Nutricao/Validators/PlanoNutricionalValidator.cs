using FluentValidation;
using TreinAI.Shared.Models;

namespace TreinAI.Api.Nutricao.Validators;

public class PlanoNutricionalValidator : AbstractValidator<PlanoNutricional>
{
    public PlanoNutricionalValidator()
    {
        RuleFor(x => x.AlunoId)
            .NotEmpty().WithMessage("AlunoId é obrigatório.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do plano é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.DataInicio)
            .NotEmpty().WithMessage("Data de início é obrigatória.");

        RuleFor(x => x.DataFim)
            .GreaterThan(x => x.DataInicio).WithMessage("Data de fim deve ser posterior à data de início.")
            .When(x => x.DataFim.HasValue);

        When(x => x.MetaDiaria != null, () =>
        {
            RuleFor(x => x.MetaDiaria!.Calorias)
                .GreaterThan(0).WithMessage("Calorias deve ser maior que zero.");
            RuleFor(x => x.MetaDiaria!.Proteinas)
                .GreaterThanOrEqualTo(0).WithMessage("Proteínas deve ser >= 0.");
            RuleFor(x => x.MetaDiaria!.Carboidratos)
                .GreaterThanOrEqualTo(0).WithMessage("Carboidratos deve ser >= 0.");
            RuleFor(x => x.MetaDiaria!.Gorduras)
                .GreaterThanOrEqualTo(0).WithMessage("Gorduras deve ser >= 0.");
        });

        RuleForEach(x => x.Refeicoes).ChildRules(r =>
        {
            r.RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome da refeição é obrigatório.");
        });
    }
}
