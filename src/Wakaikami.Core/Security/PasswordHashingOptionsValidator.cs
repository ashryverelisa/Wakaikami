using Microsoft.Extensions.Options;

namespace Wakaikami.Core.Security;

[OptionsValidator]
internal sealed partial class PasswordHashingOptionsValidator : IValidateOptions<PasswordHashingOptions>;