using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Reflection;

namespace Shimi.Shared
{
    public class DataManager<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="csvResourceName">E.g. Shimi.Shared.XXX.csv</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<T> LoadFromCsv<M>(string csvResourceName) 
            where M : ClassMap
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(csvResourceName));

            if (resourceName == null)
                throw new Exception("CSV resource not found. Check the file name and build action.");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<M>();
            return csv.GetRecords<T>().ToList();
        }
    }
}
