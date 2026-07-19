using Microsoft.Extensions.Options;

namespace Wakaikami.LoginService.Configuration;

[OptionsValidator]
internal sealed partial class LoginOptionsValidator : IValidateOptions<LoginOptions>;