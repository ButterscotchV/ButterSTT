using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ButterSTT.Config
{
    public class JsonConfigHandler<T>(string configFile, JsonTypeInfo<T> typeInfo)
    {
        public static readonly JsonSerializerOptions Options =
            new() { WriteIndented = true, TypeInfoResolver = JsonContext.Default };
        public static readonly JsonContext Context = new(Options);
        public readonly string ConfigFilePath = Path.GetFullPath(configFile);

        public T? LoadConfig(string file)
        {
            using var stream = new FileStream(file, FileMode.Open);
            return JsonSerializer.Deserialize(stream, typeInfo);
        }

        public void WriteConfigUnsafe(string file, T config)
        {
            using var stream = new FileStream(file, FileMode.OpenOrCreate);
            JsonSerializer.Serialize(stream, config, typeInfo);
        }

        public static void AtomicFileOp(
            string file,
            Action<string> operation,
            bool overwrite = false
        )
        {
            ArgumentException.ThrowIfNullOrEmpty(file);

            if (!overwrite && File.Exists(file))
                throw new IOException(
                    $"File \"{file}\" exists and \"{nameof(overwrite)}\" is set to false."
                );

            var tempFile = Path.GetTempFileName();
            operation(tempFile);
            File.Move(tempFile, file, overwrite);
        }

        public void WriteConfig(string file, T config)
        {
            AtomicFileOp(file, tempFile => WriteConfigUnsafe(tempFile, config), overwrite: true);
        }

        /// <summary>
        /// Initializes the config from the config file path or uses the default config if the file does not exist.
        /// </summary>
        /// <param name="defaultConfig">The default config to use, this value is returned if it's used.</param>
        /// <param name="logger">The logger to output to.</param>
        /// <returns>The config being used.</returns>
        public T InitializeConfig(T defaultConfig)
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    return LoadConfig(ConfigFilePath)
                        ?? throw new JsonConfigException(
                            $"Unable to load the config file at \"{ConfigFilePath}\"."
                        );
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(
                        $"{e}\nUnable to load the config file at \"{ConfigFilePath}\", backing up the current config..."
                    );

                    var backupFile = $"{ConfigFilePath}.bak";
                    try
                    {
                        AtomicFileOp(
                            backupFile,
                            tempFile => File.Copy(ConfigFilePath, tempFile, overwrite: true),
                            overwrite: true
                        );
                    }
                    catch (Exception e2)
                    {
                        throw new AggregateException(
                            new JsonConfigException(
                                $"Unable to back up the config file at \"{ConfigFilePath}\" to \"{backupFile}\".",
                                e2
                            ),
                            e
                        );
                    }

                    Console.WriteLine($"Backed up the current config file to \"{backupFile}\".");

                    // We shouldn't continue past this point as the config is required
                    throw new JsonConfigException(
                        $"Unable to load the config file at \"{ConfigFilePath}\"."
                    );
                }
            }

            WriteConfig(ConfigFilePath, defaultConfig);
            Console.WriteLine($"Generated a default config file at {ConfigFilePath}.");

            return defaultConfig;
        }
    }
}
