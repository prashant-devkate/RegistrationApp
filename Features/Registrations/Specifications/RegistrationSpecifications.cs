using RegistrationApp.Application.Interfaces;
using RegistrationApp.Models;

namespace RegistrationApp.Features.Registrations.Specifications;

/// <summary>
/// Specification for retrieving all registrations
/// </summary>
public class GetAllRegistrationsSpec : Specification<Registration>
{
    public GetAllRegistrationsSpec(int? categoryId = null, int pageNumber = 1, int pageSize = 20)
    {
        // Include related category
        AddInclude(r => r.Category!);
        AddInclude(r => r.Payments);

        // Filter by category if provided
        if (categoryId.HasValue)
        {
            AddCriteria(r => r.CategoryId == categoryId.Value);
        }

        // Order by creation date descending
        ApplyOrderByDescending(r => r.CreatedAt);

        // Apply pagination
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Specification for retrieving registration by ID
/// </summary>
public class GetRegistrationByIdSpec : Specification<Registration>
{
    public GetRegistrationByIdSpec(int registrationId)
    {
        AddCriteria(r => r.Id == registrationId);
        AddInclude(r => r.Category!);
        AddInclude(r => r.Payments);
    }
}

/// <summary>
/// Specification for retrieving registrations by status
/// </summary>
public class GetRegistrationsByStatusSpec : Specification<Registration>
{
    public GetRegistrationsByStatusSpec(RegistrationStatus status, int pageNumber = 1, int pageSize = 20)
    {
        AddCriteria(r => r.Status == status);
        AddInclude(r => r.Category!);
        AddInclude(r => r.Payments);
        ApplyOrderByDescending(r => r.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Specification for retrieving pending registrations
/// </summary>
public class GetPendingRegistrationsSpec : Specification<Registration>
{
    public GetPendingRegistrationsSpec()
    {
        AddCriteria(r => r.Status == RegistrationStatus.PaymentPending);
        AddInclude(r => r.Category!);
        AddInclude(r => r.Payments);
        ApplyOrderByDescending(r => r.CreatedAt);
    }
}
