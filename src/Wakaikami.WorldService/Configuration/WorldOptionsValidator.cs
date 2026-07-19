using Microsoft.Extensions.Options;

namespace Wakaikami.WorldService.Configuration;

[OptionsValidator]
internal sealed partial class WorldOptionsValidator : IValidateOptions<WorldOptions>;