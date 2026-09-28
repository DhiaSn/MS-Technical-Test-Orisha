using MS.SS.Core.SharedKernel.Common;

namespace MS.SS.Core.SharedKernel.Validators;

public class PasswordValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<ValidationError> Errors { get; } = [];
}
