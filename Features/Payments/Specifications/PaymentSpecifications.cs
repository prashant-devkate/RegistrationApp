using RegistrationApp.Application.Interfaces;
using RegistrationApp.Models;

namespace RegistrationApp.Features.Payments.Specifications;

/// <summary>
/// Specification for retrieving payment by ID
/// </summary>
public class GetPaymentByIdSpec : Specification<Payment>
{
    public GetPaymentByIdSpec(int paymentId)
    {
        AddCriteria(p => p.Id == paymentId);
        AddInclude(p => p.Registration!);
    }
}

/// <summary>
/// Specification for retrieving payment by Razorpay Order ID
/// </summary>
public class GetPaymentByOrderIdSpec : Specification<Payment>
{
    public GetPaymentByOrderIdSpec(string razorpayOrderId)
    {
        AddCriteria(p => p.RazorpayOrderId == razorpayOrderId);
        AddInclude(p => p.Registration!);
    }
}

/// <summary>
/// Specification for retrieving payment by Razorpay Payment ID
/// </summary>
public class GetPaymentByPaymentIdSpec : Specification<Payment>
{
    public GetPaymentByPaymentIdSpec(string razorpayPaymentId)
    {
        AddCriteria(p => p.RazorpayPaymentId == razorpayPaymentId);
        AddInclude(p => p.Registration!);
    }
}

/// <summary>
/// Specification for retrieving latest payment for a registration
/// </summary>
public class GetLatestPaymentByRegistrationSpec : Specification<Payment>
{
    public GetLatestPaymentByRegistrationSpec(int registrationId)
    {
        AddCriteria(p => p.RegistrationId == registrationId);
        ApplyOrderByDescending(p => p.CreatedAt);
        AddInclude(p => p.Registration!);
    }
}

/// <summary>
/// Specification for retrieving all payments for a registration
/// </summary>
public class GetPaymentsByRegistrationSpec : Specification<Payment>
{
    public GetPaymentsByRegistrationSpec(int registrationId)
    {
        AddCriteria(p => p.RegistrationId == registrationId);
        ApplyOrderByDescending(p => p.CreatedAt);
        AddInclude(p => p.Registration!);
    }
}

/// <summary>
/// Specification for retrieving captured payments
/// </summary>
public class GetCapturedPaymentsSpec : Specification<Payment>
{
    public GetCapturedPaymentsSpec(int pageNumber = 1, int pageSize = 20)
    {
        AddCriteria(p => p.Status == PaymentStatus.Captured);
        ApplyOrderByDescending(p => p.CreatedAt);
        AddInclude(p => p.Registration!);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Specification for retrieving failed payments
/// </summary>
public class GetFailedPaymentsSpec : Specification<Payment>
{
    public GetFailedPaymentsSpec(int pageNumber = 1, int pageSize = 20)
    {
        AddCriteria(p => p.Status == PaymentStatus.Failed);
        ApplyOrderByDescending(p => p.CreatedAt);
        AddInclude(p => p.Registration!);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
