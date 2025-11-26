namespace OpenFAST.Error
{
    public static class ErrorHandler
    {
        public static IErrorHandler Default => ErrorHandlerFields.Default;
        public static IErrorHandler Null => ErrorHandlerFields.Null;
    }
}