using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TreinAI.Api.Nutricao.Validators;
using TreinAI.Shared.Exceptions;
using TreinAI.Shared.Middleware;
using TreinAI.Shared.Models;
using TreinAI.Shared.Repositories;
using TreinAI.Shared.Validation;

namespace TreinAI.Api.Nutricao.Functions;

/// <summary>
/// CRUD for PlanoNutricional (nutritional plans).
/// </summary>
public class PlanoNutricionalFunctions
{
    private readonly IRepository<PlanoNutricional> _repository;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<PlanoNutricionalFunctions> _logger;

    public PlanoNutricionalFunctions(
        IRepository<PlanoNutricional> repository,
        TenantContext tenantContext,
        ILogger<PlanoNutricionalFunctions> logger)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [Function("GetPlanosNutricionais")]
    public async Task<HttpResponseData> GetPlanos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "nutricao")] HttpRequestData req)
    {
        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var alunoId = queryParams["alunoId"];

        IReadOnlyList<PlanoNutricional> planos;

        if (!string.IsNullOrEmpty(alunoId))
        {
            planos = await _repository.QueryAsync(
                _tenantContext.TenantId, p => p.AlunoId == alunoId);
        }
        else if (_tenantContext.IsAluno)
        {
            planos = await _repository.QueryAsync(
                _tenantContext.TenantId, p => p.AlunoId == _tenantContext.UserId);
        }
        else if (_tenantContext.IsProfessor)
        {
            planos = await _repository.QueryAsync(
                _tenantContext.TenantId, p => p.ProfessorId == _tenantContext.UserId);
        }
        else
        {
            planos = await _repository.GetAllAsync(_tenantContext.TenantId);
        }

        return await ValidationHelper.OkAsync(req, planos);
    }

    [Function("GetPlanoNutricionalById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "nutricao/{id}")] HttpRequestData req,
        string id)
    {
        var plano = await _repository.GetByIdAsync(id, _tenantContext.TenantId);
        if (plano == null)
            throw new NotFoundException("PlanoNutricional", id);

        if (_tenantContext.IsAluno && plano.AlunoId != _tenantContext.UserId)
            throw new ForbiddenException("Você só pode acessar seus próprios planos.");

        return await ValidationHelper.OkAsync(req, plano);
    }

    [Function("CreatePlanoNutricional")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "nutricao")] HttpRequestData req)
    {
        if (_tenantContext.IsAluno)
            throw new ForbiddenException("Apenas professores podem criar planos nutricionais.");

        var validator = new PlanoNutricionalValidator();
        var plano = await ValidationHelper.ValidateRequestAsync(req, validator);

        plano.TenantId = _tenantContext.TenantId;
        plano.ProfessorId = _tenantContext.UserId;
        plano.CreatedBy = _tenantContext.UserId;
        plano.UpdatedBy = _tenantContext.UserId;

        var created = await _repository.CreateAsync(plano);
        return await ValidationHelper.CreatedAsync(req, created);
    }

    [Function("UpdatePlanoNutricional")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "nutricao/{id}")] HttpRequestData req,
        string id)
    {
        if (_tenantContext.IsAluno)
            throw new ForbiddenException("Apenas professores podem editar planos nutricionais.");

        var existing = await _repository.GetByIdAsync(id, _tenantContext.TenantId);
        if (existing == null)
            throw new NotFoundException("PlanoNutricional", id);

        if (_tenantContext.IsProfessor && existing.ProfessorId != _tenantContext.UserId)
            throw new ForbiddenException("Você só pode editar planos que criou.");

        var validator = new PlanoNutricionalValidator();
        var plano = await ValidationHelper.ValidateRequestAsync(req, validator);

        plano.Id = id;
        plano.TenantId = _tenantContext.TenantId;
        plano.ProfessorId = existing.ProfessorId;
        plano.CreatedAt = existing.CreatedAt;
        plano.CreatedBy = existing.CreatedBy;
        plano.UpdatedBy = _tenantContext.UserId;

        var updated = await _repository.UpdateAsync(plano);
        return await ValidationHelper.OkAsync(req, updated);
    }

    [Function("DeletePlanoNutricional")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "nutricao/{id}")] HttpRequestData req,
        string id)
    {
        if (_tenantContext.IsAluno)
            throw new ForbiddenException("Apenas professores podem excluir planos nutricionais.");

        var existing = await _repository.GetByIdAsync(id, _tenantContext.TenantId);
        if (existing == null)
            throw new NotFoundException("PlanoNutricional", id);

        await _repository.DeleteAsync(id, _tenantContext.TenantId);
        return ValidationHelper.NoContent(req);
    }
}
