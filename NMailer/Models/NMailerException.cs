using System.Runtime.Serialization;
using System.Text;

namespace NMailer.Models;

public class NMailerException : Exception
{
    public NMailerException()
    {
    }

    protected NMailerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public NMailerException(string? message) : base(message)
    {
    }

    public NMailerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public static class ExceptionHelper
{
    public static string UnwrapMessages(this Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ex.Message);
        if (ex.InnerException is not null)
        {
            sb.AppendLine(ex.InnerException.UnwrapMessages());
        }
        return sb.ToString();
    }
}