using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// IO functions using the on-disk file system
    /// </summary>
    public partial class NopFileProvider : PhysicalFileProvider, INopFileProvider
    {
        /// <summary>
        /// Initializes a new instance of a NopFileProvider
        /// </summary>
        /// <param name="hostingEnvironment">Hosting environment</param>
        public NopFileProvider(IHostingEnvironment hostingEnvironment)
            : base(File.Exists(hostingEnvironment.WebRootPath) ? Path.GetDirectoryName(hostingEnvironment.WebRootPath) : hostingEnvironment.WebRootPath)
        {
            var path = hostingEnvironment.ContentRootPath ?? string.Empty;
            if (File.Exists(path))
                path = Path.GetDirectoryName(path);

            BaseDirectory = path;
        }

        #region Properties

        /// <summary>
        /// Gets a base directory
        /// </summary>
        protected string BaseDirectory { get; }

        #endregion

        #region Utilities

        /// <summary>
        /// Delete directories recursively
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directories are deleted</returns>
        protected virtual async Task DeleteDirectoryRecursiveAsync(string path, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                Directory.Delete(path, true);
                const int maxIterationToWait = 10;
                var curIteration = 0;

                //according to the documentation(https://msdn.microsoft.com/ru-ru/library/windows/desktop/aa365488.aspx) 
                //System.IO.Directory.Delete method ultimately (after removing the files) calls native 
                //RemoveDirectory function which marks the directory as "deleted". That's why we wait until 
                //the directory is actually deleted. For more details see https://stackoverflow.com/a/4245121
                while (Directory.Exists(path))
                {
                    curIteration += 1;
                    if (curIteration > maxIterationToWait)
                        return;
                    Thread.Sleep(100);
                }
            }, cancellationToken);
        }

        #endregion

        /// <summary>
        /// Combines an array of strings into a path
        /// </summary>
        /// <param name="paths">An array of parts of the path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains combined paths</returns>
        public virtual async Task<string> CombineAsync(IEnumerable<string> paths, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Path.Combine(paths.ToArray()), cancellationToken);
        }

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist
        /// </summary>
        /// <param name="path">The directory to create</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directory is created</returns>
        public virtual async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            if (!(await DirectoryExistsAsync(path, cancellationToken)))
                Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Creates or overwrites a file in the specified path
        /// </summary>
        /// <param name="path">The path and name of the file to create</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is created</returns>
        public virtual async Task CreateFileAsync(string path, CancellationToken cancellationToken)
        {
            if (await FileExistsAsync(path, cancellationToken))
                return;

            //we use 'using' to close the file after it's created
            using (File.Create(path)) { }
        }

        /// <summary>
        ///  Depth-first recursive delete, with handling for descendant directories open in Windows Explorer.
        /// </summary>
        /// <param name="path">Directory path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directory is deleted</returns>
        public virtual async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            //find more info about directory deletion
            //and why we use this approach at https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true

            foreach (var directory in Directory.GetDirectories(path))
            {
                await DeleteDirectoryAsync(directory, cancellationToken);
            }

            try
            {
                await DeleteDirectoryRecursiveAsync(path, cancellationToken);
            }
            catch (AggregateException exception)
            {
                if (exception.InnerException is IOException || exception.InnerException is UnauthorizedAccessException)
                    await DeleteDirectoryRecursiveAsync(path, cancellationToken);
            }
        }

        /// <summary>
        /// Deletes the specified file
        /// </summary>
        /// <param name="filePath">The name of the file to be deleted. Wildcard characters are not supported</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is deleted</returns>
        public virtual async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!(await FileExistsAsync(filePath, cancellationToken)))
                return;

            File.Delete(filePath);
        }

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether directory exists</returns>
        public virtual async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Directory.Exists(path), cancellationToken);
        }

        /// <summary>
        /// Moves a file or a directory and its contents to a new location
        /// </summary>
        /// <param name="sourceDirName">The path of the file or directory from</param>
        /// <param name="destDirName">The path of the file or directory to move</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directory is moved</returns>
        public virtual async Task DirectoryMoveAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken)
        {
            await Task.Run(() => Directory.Move(sourceDirName, destDirName), cancellationToken);
        }

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to search</param>
        /// <param name="searchPattern">The search string to match against the names of files in path. This parameter
        /// can contain a combination of valid literal path and wildcard (* and ?) characters, but doesn't support regular expressions.</param>
        /// <param name="topDirectoryOnly">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains enumerated files</returns>
        public virtual async Task<IEnumerable<string>> EnumerateFilesAsync(string directoryPath, string searchPattern,
            bool topDirectoryOnly = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() => Directory.EnumerateFiles(directoryPath, searchPattern,
                topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories), cancellationToken);
        }

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed
        /// </summary>
        /// <param name="sourceFileName">The file to copy</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory</param>
        /// <param name="overwrite">true if the destination file can be overwritten; otherwise, false</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is copied</returns>
        public virtual async Task FileCopyAsync(string sourceFileName, string destFileName, bool overwrite = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Run(() => File.Copy(sourceFileName, destFileName, overwrite), cancellationToken);
        }

        /// <summary>
        /// Determines whether the specified file exists
        /// </summary>
        /// <param name="filePath">The file to check</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether file exists</returns>
        public virtual async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() => File.Exists(filePath), cancellationToken);
        }

        /// <summary>
        /// Gets the length of the file in bytes, or -1 for a directory or non-existing files
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the length of the file</returns>
        public virtual async Task<long> FileLengthAsync(string path, CancellationToken cancellationToken)
        {
            if (!(await FileExistsAsync(path, cancellationToken)))
                return -1;

            return new FileInfo(path).Length;
        }

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name
        /// </summary>
        /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path</param>
        /// <param name="destFileName">The new path and name for the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is moved</returns>
        public virtual async Task FileMoveAsync(string sourceFileName, string destFileName, CancellationToken cancellationToken)
        {
            await Task.Run(() => File.Move(sourceFileName, destFileName), cancellationToken);
        }

        /// <summary>
        /// Returns the absolute path to the directory
        /// </summary>
        /// <param name="paths">An array of parts of the path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the absolute path to the directory</returns>
        public virtual async Task<string> GetAbsolutePathAsync(IEnumerable<string> paths, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var allPaths = paths.ToList();
                allPaths.Insert(0, Root);

                return Path.Combine(allPaths.ToArray());
            }, cancellationToken);
        }

        /// <summary>
        /// Gets an object that encapsulates the access control list (ACL) entries for a specified directory
        /// </summary>
        /// <param name="path">The path to a directory containing a System.Security.AccessControl.DirectorySecurity object that describes the file's access control list (ACL) information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the DirectorySecurity object</returns>
        public virtual async Task<DirectorySecurity> GetAccessControlAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => new DirectoryInfo(path).GetAccessControl(), cancellationToken);
        }

        /// <summary>
        /// Returns the creation date and time of the specified file or directory
        /// </summary>
        /// <param name="path">The file or directory for which to obtain creation date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the creation date and time</returns>
        public virtual async Task<DateTime> GetCreationTimeAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => File.GetCreationTime(path), cancellationToken);
        }

        /// <summary>
        /// Returns the names of the subdirectories (including their paths) that match the specified search pattern in the specified directory
        /// </summary>
        /// <param name="path">The path to the directory to search</param>
        /// <param name="searchPattern">The search string to match against the names of subdirectories in path. 
        /// This parameter can contain a combination of valid literal and wildcard characters, but doesn't support regular expressions.</param>
        /// <param name="topDirectoryOnly">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains collection of directory names</returns>
        public virtual async Task<string[]> GetDirectoriesAsync(string path, string searchPattern = "", bool topDirectoryOnly = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(searchPattern))
                    searchPattern = "*";

                return Directory.GetDirectories(path, searchPattern,
                    topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }, cancellationToken);
        }

        /// <summary>
        /// Returns the directory information for the specified path string
        /// </summary>
        /// <param name="path">The path of a file or directory</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the directory name</returns>
        public virtual async Task<string> GetDirectoryNameAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Path.GetDirectoryName(path), cancellationToken);
        }

        /// <summary>
        /// Returns the directory name only for the specified path string
        /// </summary>
        /// <param name="path">The path of directory</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the directory name</returns>
        public virtual async Task<string> GetDirectoryNameOnlyAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => new DirectoryInfo(path).Name, cancellationToken);
        }

        /// <summary>
        /// Returns the extension of the specified path string
        /// </summary>
        /// <param name="filePath">The path string from which to get the extension</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the extension of the file</returns>
        public virtual async Task<string> GetFileExtensionAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Path.GetExtension(filePath), cancellationToken);
        }

        /// <summary>
        /// Returns the file name and extension of the specified path string
        /// </summary>
        /// <param name="path">The path string from which to obtain the file name and extension</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the file name and extension</returns>
        public virtual async Task<string> GetFileNameAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Path.GetFileName(path), cancellationToken);
        }

        /// <summary>
        /// Returns the file name of the specified path string without the extension
        /// </summary>
        /// <param name="filePath">The path of the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the file name without the extension</returns>
        public virtual async Task<string> GetFileNameWithoutExtensionAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Path.GetFileNameWithoutExtension(filePath), cancellationToken);
        }

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory, using a value to determine whether to search subdirectories.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to search</param>
        /// <param name="searchPattern">The search string to match against the names of files in path. 
        /// This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but doesn't support regular expressions.</param>
        /// <param name="topDirectoryOnly">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the names of files</returns>
        public virtual async Task<string[]> GetFilesAsync(string directoryPath, string searchPattern = "", bool topDirectoryOnly = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(searchPattern))
                    searchPattern = "*.*";

                return Directory.GetFiles(directoryPath, searchPattern,
                    topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }, cancellationToken);
        }

        /// <summary>
        /// Returns the date and time the specified file or directory was last accessed
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the date and time the specified file or directory was last accessed</returns>
        public virtual async Task<DateTime> GetLastAccessTimeAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => File.GetLastAccessTime(path), cancellationToken);
        }

        /// <summary>
        /// Returns the date and time the specified file or directory was last written to
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the date and time the specified file or directory was last written to</returns>
        public virtual async Task<DateTime> GetLastWriteTimeAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => File.GetLastWriteTime(path), cancellationToken);
        }

        /// <summary>
        /// Returns the date and time, in coordinated universal time (UTC), that the specified file or directory was last written to
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the date and time (UTC) the specified file or directory was last written to</returns>
        public virtual async Task<DateTime> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() => File.GetLastWriteTimeUtc(path), cancellationToken);
        }

        /// <summary>
        /// Retrieves the parent directory of the specified path
        /// </summary>
        /// <param name="directoryPath">The path for which to retrieve the parent directory</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the parent directory</returns>
        public virtual async Task<string> GetParentDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Directory.GetParent(directoryPath).FullName, cancellationToken);
        }

        /// <summary>
        /// Checks if the path is directory
        /// </summary>
        /// <param name="path">Path for check</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether passed path is a directory</returns>
        public virtual async Task<bool> IsDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            return await DirectoryExistsAsync(path, cancellationToken);
        }

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the path</returns>
        public virtual async Task<string> MapPathAsync(string path, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                path = path.Replace("~/", string.Empty).TrimStart('/').Replace('/', '\\');
                return Path.Combine(BaseDirectory ?? string.Empty, path);
            }, cancellationToken);
        }

        /// <summary>
        /// Reads the contents of the file into a byte array
        /// </summary>
        /// <param name="filePath">The file for reading</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains a byte array containing the contents of the file</returns>
        public virtual async Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken)
        {
            return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath, cancellationToken) : new byte[0];
        }

        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading</param>
        /// <param name="encoding">The encoding applied to the contents of the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the read text from a file by the passed path</returns>
        public virtual async Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken)
        {
            return await File.ReadAllTextAsync(path, encoding, cancellationToken);
        }

        /// <summary>
        /// Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to
        /// </summary>
        /// <param name="path">The file for which to set the date and time information</param>
        /// <param name="lastWriteTimeUtc">A System.DateTime containing the value to set for the last write date and time of path. This value is expressed in UTC time</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the date and time is set</returns>
        public virtual async Task SetLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken)
        {
            await Task.Run(() => File.SetLastWriteTimeUtc(path, lastWriteTimeUtc), cancellationToken);
        }

        /// <summary>
        /// Writes the specified byte array to the file
        /// </summary>
        /// <param name="filePath">The file to write to</param>
        /// <param name="bytes">The bytes to write to the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that byte array is written to the file</returns>
        public virtual async Task WriteAllBytesAsync(string filePath, byte[] bytes, CancellationToken cancellationToken)
        {
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
        }

        /// <summary>
        /// Creates a new file, writes the specified string to the file using the specified encoding,
        /// and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to</param>
        /// <param name="contents">The string to write to the file</param>
        /// <param name="encoding">The encoding to apply to the string</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that passed text is written to a file by the passed path</returns>
        public virtual async Task WriteAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken)
        {
            await File.WriteAllTextAsync(path, contents, encoding, cancellationToken);
        }
    }
}