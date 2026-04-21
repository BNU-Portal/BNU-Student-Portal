namespace YourAPP_Shared_Library.SharedResponse
{
    public class Result
    {

        protected List<Error> errors = [];
        //list of empty errors in case of success it will be empty 
        //if there is errors gonna be added to this list and return it to the client to handle it in the client side
        public bool IsSuccess => errors.Count == 0; // if there is no errors then it is success
        public bool IsFailure => !IsSuccess; // if there is errors then it is failure
        public IReadOnlyCollection<Error> Errors => errors;
        //to be able to return the errors to the client and handle it in the client side based on the error type and the code and description of the error
        protected Result() { } //called by the Success method to create a success result with no errors
        protected Result(Error error)
        {
            this.errors.Add(error);
        }
        // Called when: Result.Fail(someError)
        // _errors = [ someError ]
        // isSuccess → false (1 error)
        // isFailure → true
        protected Result(List<Error> errors)
        {
            this.errors.AddRange(errors);
        }
        // Called when: Result.Fail(listOfErrors)
        // Used for FluentValidation — multiple broken rules at once
        // _errors = [ error1, error2, error3 ]
        // isSuccess → false
        // isFailure → true
    }

    public class  Result<T>:Result
    {
        private readonly T Value;


        public T GetValueOrThrow()
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot get the value of a failed result. Check errors for details.");
            }
            return Value;
        }

        private Result(T value) : base()   // ← calls Result() — empty errors
        {
            Value = value;
        }
        public static Result<T> Ok(T value) => new Result<T>(value);

        private Result(Error error) : base(error)  // ← calls Result(Error) — adds error
        {
            Value = default!;
        }
        public static Result<T> Fail(Error error) => new Result<T>(error);

        private Result(List<Error> errors) : base(errors)  // ← calls Result(List<Error>) — adds errors
        {
            Value = default!;
        }
        public static Result<T> Fail(List<Error> errors) => new Result<T>(errors);

        public static implicit operator Result<T>(T value) => Ok(value);
        public static implicit operator Result<T>(Error error) => Fail(error);
        public static implicit operator Result<T>(List<Error> errors) => Fail(errors);
    }
}
