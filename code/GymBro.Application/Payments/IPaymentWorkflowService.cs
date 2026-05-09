namespace GymBro.Application.Payments
{
    public interface IPaymentWorkflowService
    {
        Task<WorkflowResult> ApprovePaymentAsync(int paymentId, string adminUsername, string? adminNote);
        Task<WorkflowResult> RejectPaymentAsync(int paymentId, string adminUsername, string? rejectReason);
    }
}
