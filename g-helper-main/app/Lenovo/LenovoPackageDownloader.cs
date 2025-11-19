using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Management;
using GHelper.Helpers;

namespace GHelper.Lenovo
{
    // Package structure for Lenovo downloads
    public struct LenovoPackage
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string Description { get; init; }
        public string Version { get; init; }
        public string Category { get; init; }
        public string FileName { get; init; }
        public string FileSize { get; init; }
        public string? FileCrc { get; init; }
        public DateTime ReleaseDate { get; init; }
        public string? Readme { get; init; }
        public string FileLocation { get; init; }
        public bool IsUpdate { get; init; }
    }

    // OS enum for Lenovo package downloaders
    public enum LenovoOS
    {
        Windows11,
        Windows10,
        Windows8,
        Windows7
    }

    // Base abstract class for package downloaders
    public abstract class AbstractLenovoPackageDownloader
    {
        protected HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        public abstract Task<List<LenovoPackage>> GetPackagesAsync(string machineType, LenovoOS os, IProgress<float>? progress = null, CancellationToken token = default);

        public async Task<string> DownloadPackageFileAsync(LenovoPackage package, string location, IProgress<float>? progress = null, CancellationToken token = default)
        {
            using var httpClient = CreateHttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            using (var response = await httpClient.GetAsync(package.FileLocation, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var contentLength = response.Content.Headers.ContentLength;
                
                await using (var fileStream = System.IO.File.OpenWrite(tempPath))
                await using (var download = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                {
                    if (progress != null && contentLength.HasValue)
                    {
                        var buffer = new byte[81920];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                            totalBytesRead += bytesRead;
                            progress.Report((float)totalBytesRead / contentLength.Value);
                        }
                    }
                    else
                    {
                        await download.CopyToAsync(fileStream, token).ConfigureAwait(false);
                    }
                }
            }

            await TryValidateChecksum(package, tempPath, httpClient, token).ConfigureAwait(false);

            var filename = SanitizeFileName(package.Title) + " - " + package.FileName;
            var finalPath = System.IO.Path.Combine(location, filename);

            System.IO.File.Move(tempPath, finalPath, true);

            return finalPath;
        }

        private static async Task TryValidateChecksum(LenovoPackage package, string tempPath, HttpClient httpClient, CancellationToken token)
        {
            if (string.IsNullOrEmpty(package.FileCrc))
            {
                Logger.WriteLine($"LenovoPackageDownloader: No CRC provided for {package.FileName}, skipping checksum validation.");
                return;
            }

            await using var fileStream = System.IO.File.OpenRead(tempPath);
            using var managedSha256 = System.Security.Cryptography.SHA256.Create();

            var fileSha256Bytes = await managedSha256.ComputeHashAsync(fileStream, token).ConfigureAwait(false);
            var fileSha256 = fileSha256Bytes.Aggregate(string.Empty, (current, b) => current + b.ToString("X2"));

            if (fileSha256.Equals(package.FileCrc, StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.WriteLine($"LenovoPackageDownloader: Package file checksum match for {package.FileName}.");
                return;
            }

            Logger.WriteLine($"LenovoPackageDownloader: File checksum mismatch for {package.FileName}. Expected: {package.FileCrc}, Got: {fileSha256}");
            throw new System.IO.InvalidDataException("File checksum mismatch");
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }

    // PCSupport package downloader (modern API)
    public class LenovoPCSupportPackageDownloader : AbstractLenovoPackageDownloader
    {
        private const string CATALOG_BASE_URL = "https://pcsupport.lenovo.com/us/en/api/v4/downloads/drivers?productId=";

        public override async Task<List<LenovoPackage>> GetPackagesAsync(string machineType, LenovoOS os, IProgress<float>? progress = null, CancellationToken token = default)
        {
            var osString = os switch
            {
                LenovoOS.Windows11 => "Windows 11",
                LenovoOS.Windows10 => "Windows 10",
                LenovoOS.Windows8 => "Windows 8",
                LenovoOS.Windows7 => "Windows 7",
                _ => throw new InvalidOperationException(nameof(os)),
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://pcsupport.lenovo.com/");
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            progress?.Report(0);

            try
            {
                var url = $"{CATALOG_BASE_URL}{machineType}";
                Logger.WriteLine($"LenovoPCSupport: Fetching packages from URL: {url}");
                Logger.WriteLine($"LenovoPCSupport: machineType={machineType}, OS={osString}");
                
                var catalogJson = await httpClient.GetStringAsync(url, token).ConfigureAwait(false);
                
                if (string.IsNullOrEmpty(catalogJson))
                {
                    Logger.WriteLine("LenovoPCSupport: Empty response from API");
                    return new List<LenovoPackage>();
                }
                
                Logger.WriteLine($"LenovoPCSupport: Response length: {catalogJson.Length} chars");
                
                var catalogJsonNode = JsonNode.Parse(catalogJson);
                var bodyNode = catalogJsonNode?["body"];
                var downloadsNode = bodyNode?["DownloadItems"]?.AsArray();

                if (downloadsNode is null)
                {
                    Logger.WriteLine($"LenovoPCSupport: No downloads found. Response structure: body={bodyNode != null}, DownloadItems={bodyNode?["DownloadItems"] != null}");
                    if (catalogJsonNode != null)
                    {
                        Logger.WriteLine($"LenovoPCSupport: Response keys: {string.Join(", ", catalogJsonNode.AsObject().Select(k => k.Key))}");
                    }
                    return new List<LenovoPackage>();
                }
                
                Logger.WriteLine($"LenovoPCSupport: Found {downloadsNode.Count} download items in response");

                var packages = new List<LenovoPackage>();
                foreach (var downloadNode in downloadsNode)
                {
                    if (token.IsCancellationRequested)
                        break;

                    if (!IsCompatible(downloadNode, osString))
                        continue;

                    var package = ParsePackage(downloadNode!);
                    if (package.HasValue)
                    {
                        packages.Add(package.Value);
                    }
                }

                Logger.WriteLine($"LenovoPCSupport: Found {packages.Count} packages");
                progress?.Report(1.0f);
                return packages;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoPCSupport: Error fetching packages: {ex.Message}");
                throw;
            }
        }

        public async Task<string> DownloadPackageFileAsync(LenovoPackage package, string location, IProgress<float>? progress = null, CancellationToken token = default)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30);

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                using (var response = await httpClient.GetAsync(package.FileLocation, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    await using (var fileStream = System.IO.File.OpenWrite(tempPath))
                    await using (var download = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                    {
                        if (progress != null && contentLength.HasValue)
                        {
                            var buffer = new byte[81920];
                            long totalBytesRead = 0;
                            int bytesRead;

                            while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                                totalBytesRead += bytesRead;
                                progress.Report((float)totalBytesRead / contentLength.Value);
                            }
                        }
                        else
                        {
                            await download.CopyToAsync(fileStream, token).ConfigureAwait(false);
                        }
                    }
                }

                var filename = SanitizeFileName(package.Title) + " - " + package.FileName;
                var finalPath = System.IO.Path.Combine(location, filename);

                System.IO.File.Move(tempPath, finalPath, true);
                Logger.WriteLine($"LenovoPCSupport: Downloaded {filename} to {finalPath}");

                return finalPath;
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
                Logger.WriteLine($"LenovoPCSupport: Error downloading package: {ex.Message}");
                throw;
            }
        }

        private static LenovoPackage? ParsePackage(JsonNode downloadNode)
        {
            try
            {
                var id = downloadNode["ID"]?.ToString() ?? "";
                var category = downloadNode["Category"]?["Name"]?.ToString() ?? "";
                var title = downloadNode["Title"]?.ToString() ?? "";
                var description = downloadNode["Summary"]?.ToString() ?? "";
                var version = downloadNode["SummaryInfo"]?["Version"]?.ToString() ?? "";

                var filesNode = downloadNode["Files"]?.AsArray();
                if (filesNode == null || filesNode.Count == 0)
                    return null;

                var mainFileNode = filesNode.FirstOrDefault(n => n?["TypeString"]?.ToString().Equals("exe", StringComparison.InvariantCultureIgnoreCase) == true)
                                   ?? filesNode.FirstOrDefault(n => n?["TypeString"]?.ToString().Equals("zip", StringComparison.InvariantCultureIgnoreCase) == true)
                                   ?? filesNode.FirstOrDefault();

                if (mainFileNode is null)
                    return null;

                var fileLocation = mainFileNode["URL"]?.ToString();
                if (string.IsNullOrEmpty(fileLocation))
                    return null;

                var fileName = new Uri(fileLocation).Segments.LastOrDefault("file");
                var fileSize = mainFileNode["Size"]?.ToString() ?? "";
                var fileCrc = mainFileNode["SHA256"]?.ToString();
                
                DateTime releaseDate = DateTime.Now;
                if (mainFileNode["Date"]?["Unix"] != null && long.TryParse(mainFileNode["Date"]["Unix"].ToString(), out var unixTime))
                {
                    releaseDate = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
                }

                var readmeFileNode = filesNode.FirstOrDefault(n => n?["TypeString"]?.ToString().Equals("txt readme", StringComparison.InvariantCultureIgnoreCase) == true)
                                      ?? filesNode.FirstOrDefault(n => n?["TypeString"]?.ToString().Equals("html", StringComparison.InvariantCultureIgnoreCase) == true);

                var readme = readmeFileNode?["URL"]?.ToString();

                return new LenovoPackage
                {
                    Id = id,
                    Title = title,
                    Description = title == description ? string.Empty : description,
                    Version = version,
                    Category = category,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileCrc = fileCrc,
                    ReleaseDate = releaseDate,
                    Readme = readme,
                    FileLocation = fileLocation,
                    IsUpdate = false // PCSupport doesn't detect updates
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoPCSupport: Error parsing package: {ex.Message}");
                return null;
            }
        }

        private static bool IsCompatible(JsonNode? downloadNode, string osString)
        {
            var operatingSystems = downloadNode?["OperatingSystemKeys"]?.AsArray();

            if (operatingSystems is null || operatingSystems.Count == 0)
                return true;

            foreach (var operatingSystem in operatingSystems)
            {
                if (operatingSystem != null && operatingSystem.ToString().StartsWith(osString, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }

    // Helper class to get machineType from WMI
    public static class LenovoMachineInfo
    {
        public static (string vendor, string machineType, string model, string serialNumber) GetMachineData()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct"))
                {
                    foreach (System.Management.ManagementObject obj in searcher.Get())
                    {
                        var vendor = obj["Vendor"]?.ToString() ?? "";
                        var version = obj["Version"]?.ToString() ?? "";
                        var model = obj["Name"]?.ToString() ?? "";
                        var serialNumber = obj["IdentifyingNumber"]?.ToString() ?? "";
                        
                        // Try to extract machineType
                        string machineType = "";
                        
                        // First, try to use model if it's in machineType format (4 alphanumeric chars, e.g., "83LT")
                        if (model.Length >= 4 && System.Text.RegularExpressions.Regex.IsMatch(model.Substring(0, 4), @"^[A-Z0-9]{4}$"))
                        {
                            machineType = model.Substring(0, 4);
                            Logger.WriteLine($"LenovoMachineInfo: Using machineType from model: {machineType}");
                        }
                        // Second, try Version field if it's in machineType format
                        else if (version.Length >= 4 && System.Text.RegularExpressions.Regex.IsMatch(version.Substring(0, 4), @"^[A-Z0-9]{4}$"))
                        {
                            machineType = version.Substring(0, 4);
                            Logger.WriteLine($"LenovoMachineInfo: Using machineType from Version: {machineType}");
                        }
                        // Third, try to extract from model string (look for pattern like "83LT" in model name)
                        else
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(model, @"\b([A-Z0-9]{4})\b");
                            if (match.Success)
                            {
                                machineType = match.Groups[1].Value;
                                Logger.WriteLine($"LenovoMachineInfo: Extracted machineType from model: {machineType}");
                            }
                            else
                            {
                                // Fallback: try to extract from version
                                match = System.Text.RegularExpressions.Regex.Match(version, @"\b([A-Z0-9]{4})\b");
                                if (match.Success)
                                {
                                    machineType = match.Groups[1].Value;
                                    Logger.WriteLine($"LenovoMachineInfo: Extracted machineType from Version: {machineType}");
                                }
                            }
                        }
                        
                        if (string.IsNullOrEmpty(machineType))
                        {
                            Logger.WriteLine($"LenovoMachineInfo: Could not extract machineType. Model={model}, Version={version}");
                        }
                        
                        return (vendor, machineType, model, serialNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoMachineInfo: Error getting machine data: {ex.Message}");
            }
            return ("", "", "", "");
        }

        public static LenovoOS GetCurrentOS()
        {
            var version = Environment.OSVersion.Version;
            if (version.Major == 10 && version.Build >= 22000)
                return LenovoOS.Windows11;
            else if (version.Major == 10)
                return LenovoOS.Windows10;
            else if (version.Major == 6 && version.Minor == 2)
                return LenovoOS.Windows8;
            else if (version.Major == 6 && version.Minor == 1)
                return LenovoOS.Windows7;
            else
                return LenovoOS.Windows10; // Default fallback
        }
    }

    // Vantage package downloader (XML catalog with update detection)
    // Note: Full VantagePackageUpdateDetector implementation is complex and requires many rules.
    // This is a simplified version that loads packages but doesn't fully detect installed drivers.
    public class LenovoVantagePackageDownloader : AbstractLenovoPackageDownloader
    {
        private const string CATALOG_BASE_URL = "https://download.lenovo.com/catalog/";

        public override async Task<List<LenovoPackage>> GetPackagesAsync(string machineType, LenovoOS os, IProgress<float>? progress = null, CancellationToken token = default)
        {
            progress?.Report(0);

            var osString = os switch
            {
                LenovoOS.Windows11 => "win11",
                LenovoOS.Windows10 => "win10",
                LenovoOS.Windows8 => "win8",
                LenovoOS.Windows7 => "win7",
                _ => throw new ArgumentOutOfRangeException(nameof(os), os, null)
            };

            using var httpClient = CreateHttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var catalogUrl = $"{CATALOG_BASE_URL}{machineType}_{osString}.xml";
            Logger.WriteLine($"LenovoVantage: Fetching catalog from: {catalogUrl}");

            try
            {
                var catalogString = await httpClient.GetStringAsync(catalogUrl, token).ConfigureAwait(false);
                var document = new XmlDocument();
                document.LoadXml(catalogString);

                var packageNodes = document.SelectNodes("/packages/package");
                if (packageNodes is null)
                {
                    Logger.WriteLine("LenovoVantage: No packages found in catalog");
                    return new List<LenovoPackage>();
                }

                var packages = new List<LenovoPackage>();
                var count = 0;
                var totalCount = packageNodes.Count;

                // Build driver info cache for real update detection
                var driverInfoCache = await BuildDriverInfoCacheAsync().ConfigureAwait(false);

                foreach (XmlElement packageNode in packageNodes)
                {
                    if (token.IsCancellationRequested) break;

                    var pLocation = packageNode.SelectSingleNode("location")?.InnerText;
                    var pCategory = packageNode.SelectSingleNode("category")?.InnerText;

                    if (string.IsNullOrWhiteSpace(pLocation) || string.IsNullOrWhiteSpace(pCategory))
                        continue;

                    try
                    {
                        var package = await GetPackageAsync(httpClient, driverInfoCache, pLocation, pCategory, token).ConfigureAwait(false);
                        if (package.HasValue)
                        {
                            // Perform real update detection using driver cache
                            var isUpdate = CheckIfPackageIsUpdate(package.Value, driverInfoCache);
                            // Create package with correct IsUpdate status
                            var packageWithStatus = new LenovoPackage
                            {
                                Id = package.Value.Id,
                                Title = package.Value.Title,
                                Description = package.Value.Description,
                                Version = package.Value.Version,
                                Category = package.Value.Category,
                                FileName = package.Value.FileName,
                                FileSize = package.Value.FileSize,
                                FileCrc = package.Value.FileCrc,
                                ReleaseDate = package.Value.ReleaseDate,
                                Readme = package.Value.Readme,
                                FileLocation = package.Value.FileLocation,
                                IsUpdate = isUpdate
                            };
                            packages.Add(packageWithStatus);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"LenovoVantage: Error loading package from {pLocation}: {ex.Message}");
                    }

                    count++;
                    progress?.Report((float)count / totalCount);
                }

                Logger.WriteLine($"LenovoVantage: Found {packages.Count} packages");
                return packages;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("NotFound"))
            {
                Logger.WriteLine($"LenovoVantage: Catalog not found for {machineType}_{osString}");
                return new List<LenovoPackage>();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoVantage: Error fetching catalog: {ex.Message}");
                return new List<LenovoPackage>();
            }
        }

        private static async Task<List<DriverInfo>> BuildDriverInfoCacheAsync()
        {
            var driverInfoCache = new List<DriverInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var deviceId = obj["DeviceID"]?.ToString() ?? string.Empty;
                    var hardwareId = obj["HardWareId"]?.ToString() ?? string.Empty;
                    var driverVersionString = obj["DriverVersion"]?.ToString();
                    var driverDateString = obj["DriverDate"]?.ToString();

                    Version? driverVersion = null;
                    if (!string.IsNullOrEmpty(driverVersionString) && Version.TryParse(driverVersionString, out var v))
                        driverVersion = v;

                    DateTime? driverDate = null;
                    if (!string.IsNullOrEmpty(driverDateString))
                    {
                        try
                        {
                            driverDate = ManagementDateTimeConverter.ToDateTime(driverDateString).Date;
                        }
                        catch { }
                    }

                    driverInfoCache.Add(new DriverInfo(deviceId, hardwareId, driverVersion, driverDate));
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoVantage: Error building driver cache: {ex.Message}");
            }
            return driverInfoCache;
        }

        private static async Task<LenovoPackage?> GetPackageAsync(HttpClient httpClient, List<DriverInfo> driverInfoCache, string location, string category, CancellationToken token)
        {
            try
            {
                var baseLocation = location.Remove(location.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase));
                var packageString = await httpClient.GetStringAsync(location, token).ConfigureAwait(false);

                var document = new XmlDocument();
                document.LoadXml(packageString);

                var id = document.SelectSingleNode("/Package/@id")?.InnerText ?? "";
                var title = document.SelectSingleNode("/Package/Title/Desc")?.InnerText ?? "";
                var version = document.SelectSingleNode("/Package/@version")?.InnerText ?? "";
                var fileName = document.SelectSingleNode("/Package/Files/Installer/File/Name")?.InnerText ?? "";
                var fileCrc = document.SelectSingleNode("/Package/Files/Installer/File/CRC")?.InnerText;
                var fileSizeBytes = int.TryParse(document.SelectSingleNode("/Package/Files/Installer/File/Size")?.InnerText, out var size) ? size : 0;
                var fileSize = $"{fileSizeBytes / 1024.0 / 1024.0:0.00} MB";
                var releaseDateString = document.SelectSingleNode("/Package/ReleaseDate")?.InnerText ?? "";
                var releaseDate = DateTime.TryParse(releaseDateString, out var date) ? date : DateTime.Now;
                var readmeName = document.SelectSingleNode("/Package/Files/Readme/File/Name")?.InnerText;
                var readme = readmeName != null ? $"{baseLocation}/{readmeName}" : null;
                var fileLocation = $"{baseLocation}/{fileName}";

                // Return package without IsUpdate - it will be set later using real driver check
                return new LenovoPackage
                {
                    Id = id,
                    Title = title,
                    Description = string.Empty,
                    Version = version,
                    Category = category,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileCrc = fileCrc,
                    ReleaseDate = releaseDate,
                    Readme = readme,
                    FileLocation = fileLocation,
                    IsUpdate = false // Will be set by real check later
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoVantage: Error parsing package from {location}: {ex.Message}");
                return null;
            }
        }

        // Helper struct for driver info
        private struct DriverInfo
        {
            public string DeviceId { get; }
            public string HardwareId { get; }
            public Version? Version { get; }
            public DateTime? Date { get; }

            public DriverInfo(string deviceId, string hardwareId, Version? version, DateTime? date)
            {
                DeviceId = deviceId;
                HardwareId = hardwareId;
                Version = version;
                Date = date;
            }
        }

        // Real update detection using driver cache (similar to Updates.cs)
        private static bool CheckIfPackageIsUpdate(LenovoPackage package, List<DriverInfo> driverInfoCache)
        {
            try
            {
                // Extract key words from title for matching
                var keywords = ExtractDriverKeywords(package.Title, package.Category);

                DriverInfo? matchedDriver = null;

                // Try to find matching driver by keywords
                foreach (var keyword in keywords)
                {
                    foreach (var driver in driverInfoCache)
                    {
                        if (driver.DeviceId.ToLowerInvariant().Contains(keyword) ||
                            driver.HardwareId.ToLowerInvariant().Contains(keyword))
                        {
                            matchedDriver = driver;
                            break;
                        }
                    }
                    if (matchedDriver.HasValue) break;
                }

                if (!matchedDriver.HasValue)
                {
                    // No matching driver found - assume it's an update (new driver)
                    return true;
                }

                // Compare versions
                var packageVersion = ParseVersionFromString(package.Version);
                if (packageVersion == null)
                {
                    // Can't parse version - assume it's an update
                    return true;
                }

                if (matchedDriver.Value.Version == null)
                {
                    // Installed driver has no version - assume package is newer
                    return true;
                }

                // Compare versions
                var isUpdate = packageVersion > matchedDriver.Value.Version;
                Logger.WriteLine($"LenovoVantage: Package '{package.Title}' - Package version: {packageVersion}, Installed: {matchedDriver.Value.Version}, IsUpdate: {isUpdate}");
                return isUpdate;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoVantage: Error checking if package is update: {ex.Message}");
                return true; // Default to showing as update if we can't determine
            }
        }

        private static List<string> ExtractDriverKeywords(string title, string category)
        {
            var keywords = new List<string>();

            // Extract manufacturer/vendor names
            var manufacturers = new[] { "nvidia", "amd", "intel", "realtek", "mediatek", "lenovo", "dolby" };
            foreach (var mfg in manufacturers)
            {
                if (title.ToLowerInvariant().Contains(mfg))
                    keywords.Add(mfg);
            }

            // Extract device types from category
            if (category.ToLowerInvariant().Contains("display") || category.ToLowerInvariant().Contains("video") || category.ToLowerInvariant().Contains("graphics"))
            {
                keywords.Add("display");
                keywords.Add("vga");
            }
            if (category.ToLowerInvariant().Contains("audio") || title.ToLowerInvariant().Contains("audio"))
                keywords.Add("audio");
            if (category.ToLowerInvariant().Contains("network") || category.ToLowerInvariant().Contains("lan") || category.ToLowerInvariant().Contains("wlan"))
                keywords.Add("network");
            if (category.ToLowerInvariant().Contains("bluetooth"))
                keywords.Add("bluetooth");
            if (category.ToLowerInvariant().Contains("camera"))
                keywords.Add("camera");

            return keywords;
        }

        private static Version? ParseVersionFromString(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
                return null;

            try
            {
                // Try to extract version number from string like "32.0.13036.5006 WHQL" or "V2.0.27"
                var cleanVersion = versionString.Trim();
                
                // Remove common prefixes
                if (cleanVersion.StartsWith("V", StringComparison.InvariantCultureIgnoreCase))
                    cleanVersion = cleanVersion.Substring(1);
                
                // Extract first part that looks like a version (numbers and dots)
                var match = System.Text.RegularExpressions.Regex.Match(cleanVersion, @"(\d+\.\d+(?:\.\d+)*(?:\.\d+)*)");
                if (match.Success)
                {
                    if (Version.TryParse(match.Groups[1].Value, out var version))
                        return version;
                }

                // Try parsing the whole string
                if (Version.TryParse(cleanVersion, out var fullVersion))
                    return fullVersion;
            }
            catch { }

            return null;
        }
    }
}

