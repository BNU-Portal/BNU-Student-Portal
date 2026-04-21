namespace YourAPP_Shared_Library.SharedResponse
{


    public enum ErrorType : byte
    {
        Failure = 0,
        Validation = 1,
        NotFound = 2,
        Unauthorized = 3,
        Forbidden = 4,
        InternalServerError = 5,
        BadRequest = 6,
        InvalidCredentials = 7,
    }
    public class Error // Represent the Error we gonna use for the Result class to be able
                       // to return the error to the client and handle it in the client side based on the error type and the code and description of the error
    {


        //what i need from this class is to have the error type and the status code of the error and some description of the error
        //to be able to return it to the client and handle it in the client side
        public ErrorType Type { get; } 

        public string Description { get; }

        public string Code { get; }



        private Error(string code, string description, ErrorType type)
        {
            Code = code;
            Description = description;
            this.Type = type;
        }
        //create the error with the code and the description and the type of the error
        //using/the factory method to create the error based on every error type and the code and the description
        #region Factory Methods of Errors 

        //in every usage in the service layer we will assign the code and description of the error and
        //we gonna return the error to the client and handle it in the client side based on the error type and the code and description of the error

        public static Error Failure(string code = "General.Failure", string description = "A General Failure Occurred ")
        {
            return new Error(code, description, ErrorType.Failure);
        }

        public static Error Validation(string code, string description)
        {
            return new Error(code, description, ErrorType.Validation);
        }
        public static Error NotFound(string code, string description)
        {
            return new Error(code, description, ErrorType.NotFound);
        }

        public static Error Unauthorized(string code, string description)
        {
            return new Error(code, description, ErrorType.Unauthorized);
        }

        public static Error Forbidden(string code, string description)
        {
            return new Error(code, description, ErrorType.Forbidden);
        }

        public static Error InternalServerError(string code, string description)
        {
            return new Error(code, description, ErrorType.InternalServerError);
        }

        public static Error BadRequest(string code, string description)
        {
            return new Error(code, description, ErrorType.BadRequest);
        }

        public static Error InvalidCredentials(string code, string description)
        {
            return new Error(code, description, ErrorType.InvalidCredentials);
        }



        #endregion

    }
}
