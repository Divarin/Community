using miniBBS.Basic.Models;

namespace miniBBS.Basic.Executors
{
    public static class Range
    {
        public static int?[] Parse(string line, Variables variables)
        {
            int i;

            int?[] result = new int?[] { null, null };
            
            if (string.IsNullOrWhiteSpace(line))
                return result;

            line = line.Trim();
            if (line.Contains("-"))
            {
                var parts = line.Split('-');

                for (int x = 0; x <= 1; x++)
                {
                    if (!string.IsNullOrWhiteSpace(parts[x]))
                    {
                        if (int.TryParse(parts[x], out i))
                            result[x] = i;
                        else if (true == variables.Labels?.ContainsKey(parts[x]))
                            result[x] = variables.Labels[parts[x]];
                    }
                }

                return result;
            }

            if (int.TryParse(line, out i))
            {
                result[0] = i;
                result[1] = i;
            }
            else if (true == variables.Labels?.ContainsKey(line))
            {
                i = variables.Labels[line];
                result[0] = i;
                result[1] = i;
            }
            return result;

        }
    }
}
