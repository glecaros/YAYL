using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace YAYL;

internal record YamlVariableResolver(Regex Expression, Func<string, CancellationToken, Task<string>> Resolver);
