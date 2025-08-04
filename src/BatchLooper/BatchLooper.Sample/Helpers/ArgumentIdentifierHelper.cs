namespace BatchLooper.Sample.Helpers
{
    public static class ArgumentIdentifierHelper
    {
        /// <summary>
        /// Pega o valor do argumento passado.
        /// Quando "valueIdentifier" contiver no argumento o seu valor será o valor após "valueIdentifier".
        /// quando não quantiver, pegara como valor o proximo argumento.
        /// </summary>
        /// <param name="args">A lista de argumentos.</param>
        /// <param name="arg">O argumento que será procurado na lista.</param>
        /// <param name="value">Valor do argumento.</param>
        /// <param name="valueIdentifier">Caracter que identificará o valor do argumnto.</param>
        /// <returns>True se o argumento existir, false quand não existir.</returns>
        public static bool IdentifyArg(this IEnumerable<string> args, string arg, out string value, string valueIdentifier = ":")
        {
            value = string.Empty;
            var tmpArg = args
                .FirstOrDefault(x => !string.IsNullOrEmpty(x?.Trim()) && (x.Trim().Equals(arg?.Trim(), StringComparison.OrdinalIgnoreCase) || x.Trim().Contains((arg ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(tmpArg))
            {
                if (tmpArg.Contains(valueIdentifier))
                    value = tmpArg[(tmpArg.IndexOf(valueIdentifier) + 1)..]?.Trim() ?? string.Empty;
                else
                {
                    var idx = (args.ToList().FindIndex(x => x.Equals(tmpArg)) + 1);
                    if (idx >= 0 && idx < args.Count())
                        value = args.ElementAt(idx)?.Trim() ?? string.Empty;
                }
            }

            return !string.IsNullOrEmpty(tmpArg);
        }
    }
}
