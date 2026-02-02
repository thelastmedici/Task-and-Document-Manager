namespace System.Net.Mail{
namespace TaskAndDocumentManager.Apploication.Auth.UseCases
    {
        public class RegisterUser
        {
            //Email validation
            private bool IsValidEmail(string email)
            {
                var trimmedEmial = email.Trim();

                if (trimmedEmial.EndsWith("."))
                {
                    return false;
                }
                try
                {
                    var addr = new MailAddress(email);
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}






// namespace TaskAndDocumentManager.Application.Auth.UseCases
// {
// 	public class RegisterUser
// 	{
// 		// Todo email validation
// 		private bool IsValidEmail(string email)
// 		{
// 			// TODO: Implement email validation
// 			return true;
// 		}

// 		// Password hashing
// 		private string HashPassword(string password)
// 		{
// 			// TODO: Implement password hashing
// 			return password;
// 		}

// 		// Saving users
// 		public void SaveUser(string email, string password)
// 		{
// 			// TODO: Implement user saving logic
// 		}

// 		// It proves your architecture works
// 	}
// }