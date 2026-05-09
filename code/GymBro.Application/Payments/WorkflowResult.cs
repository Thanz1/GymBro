namespace GymBro.Application.Payments
{
    public class WorkflowResult
    {
        public bool Succeeded { get; private set; }
        public bool IsWarning { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public static WorkflowResult Success(string message)
        {
            return new WorkflowResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static WorkflowResult Warning(string message)
        {
            return new WorkflowResult
            {
                IsWarning = true,
                Message = message
            };
        }

        public static WorkflowResult Failure(string message)
        {
            return new WorkflowResult
            {
                Message = message
            };
        }
    }
}
