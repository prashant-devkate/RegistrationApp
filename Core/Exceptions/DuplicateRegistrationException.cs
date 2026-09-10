namespace RegistrationApp.Core.Exceptions
{
    public class DuplicateRegistrationException : Exception
    {
        public string PhoneNumber { get; }

        public DuplicateRegistrationException(string phoneNumber)
            : base($"A registration with phone number {phoneNumber} already exists.")
        {
            PhoneNumber = phoneNumber;
        }
    }
}
