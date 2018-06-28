using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// A file provider abstraction
    /// </summary>
    public partial interface INopFileProvider : IFileProvider
    {
        /// <summary>
        /// Combines an array of strings into a path
        /// </summary>
        /// <param name="paths">An array of parts of the path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains combined paths</returns>
        Task<string> CombineAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist
        /// </summary>
        /// <param name="path">The directory to create</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directory is created</returns>
        Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates or overwrites a file in the specified path
        /// </summary>
        /// <param name="path">The path and name of the file to create</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is created</returns>
        Task CreateFileAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///  Depth-first recursive delete, with handling for descendant directories open in Windows Explorer.
        /// </summary>
        /// <param name="path">Directory path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directory is deleted</returns>
        Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes the specified file
        /// </summary>
        /// <param name="filePath">The name of the file to be deleted. Wildcard characters are not supported</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is deleted</returns>
        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether directory exists</returns>
        Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Moves a file or a directory and its contents to a new location
        /// </summary>
        /// <param name="sourceDirName">The path of the file or directory from</param>
        /// <param name="destDirName">The path of the file or directory to move</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that directory is moved</returns>
        Task DirectoryMoveAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to search</param>
        /// <param name="searchPattern">The search string to match against the names of files in path. This parameter
        /// can contain a combination of valid literal path and wildcard (* and ?) characters, but doesn't support regular expressions./// </param>
        /// <param name="topDirectoryOnly">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains enumerated files</returns>
        Task<IEnumerable<string>> EnumerateFilesAsync(string directoryPath, string searchPattern,
            bool topDirectoryOnly = true, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed
        /// </summary>
        /// <param name="sourceFileName">The file to copy</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory</param>
        /// <param name="overwrite">true if the destination file can be overwritten; otherwise, false</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is copied</returns>
        Task FileCopyAsync(string sourceFileName, string destFileName, bool overwrite = false,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Determines whether the specified file exists
        /// </summary>
        /// <param name="filePath">The file to check</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether file exists</returns>
        Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the length of the file in bytes, or -1 for a directory or non-existing files
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>The length of the file</returns>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the length of the file</returns>
        Task<long> FileLengthAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name
        /// </summary>
        /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path</param>
        /// <param name="destFileName">The new path and name for the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that file is moved</returns>
        Task FileMoveAsync(string sourceFileName, string destFileName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the absolute path to the directory
        /// </summary>
        /// <param name="paths">An array of parts of the path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the absolute path to the directory</returns>
        Task<string> GetAbsolutePathAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets an object that encapsulates the access control list (ACL) entries for a specified directory
        /// </summary>
        /// <param name="path">The path to a directory containing a System.Security.AccessControl.DirectorySecurity object that describes the file's access control list (ACL) information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the DirectorySecurity object</returns>
        Task<DirectorySecurity> GetAccessControlAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the creation date and time of the specified file or directory
        /// </summary>
        /// <param name="path">The file or directory for which to obtain creation date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the creation date and time</returns>
        Task<DateTime> GetCreationTimeAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the names of the subdirectories (including their paths) that match the specified search pattern in the specified directory
        /// </summary>
        /// <param name="path">The path to the directory to search</param>
        /// <param name="searchPattern">The search string to match against the names of subdirectories in path. 
        /// This parameter can contain a combination of valid literal and wildcard characters, but doesn't support regular expressions.</param>
        /// <param name="topDirectoryOnly">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains collection of directory names</returns>
        Task<string[]> GetDirectoriesAsync(string path, string searchPattern = "", bool topDirectoryOnly = true,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the directory information for the specified path string
        /// </summary>
        /// <param name="path">The path of a file or directory</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the directory name</returns>
        Task<string> GetDirectoryNameAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the directory name only for the specified path string
        /// </summary>
        /// <param name="path">The path of directory</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the directory name</returns>
        Task<string> GetDirectoryNameOnlyAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the extension of the specified path string
        /// </summary>
        /// <param name="filePath">The path string from which to get the extension</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the extension of the file</returns>
        Task<string> GetFileExtensionAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the file name and extension of the specified path string
        /// </summary>
        /// <param name="path">The path string from which to obtain the file name and extension</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the file name and extension</returns>
        Task<string> GetFileNameAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the file name of the specified path string without the extension
        /// </summary>
        /// <param name="filePath">The path of the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the file name without the extension</returns>
        Task<string> GetFileNameWithoutExtensionAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory, using a value to determine whether to search subdirectories.
        /// </summary>
        /// <param name="directoryPath">The path to the directory to search</param>
        /// <param name="searchPattern">The search string to match against the names of files in path. 
        /// This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but doesn't support regular expressions.</param>
        /// <param name="topDirectoryOnly">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the names of files</returns>
        Task<string[]> GetFilesAsync(string directoryPath, string searchPattern = "", bool topDirectoryOnly = true,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the date and time the specified file or directory was last accessed
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the date and time the specified file or directory was last accessed</returns>
        Task<DateTime> GetLastAccessTimeAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the date and time the specified file or directory was last written to
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the date and time the specified file or directory was last written to</returns>
        Task<DateTime> GetLastWriteTimeAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the date and time, in coordinated universal time (UTC), that the specified file or directory was last written to
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the date and time (UTC) the specified file or directory was last written to</returns>
        Task<DateTime> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Retrieves the parent directory of the specified path
        /// </summary>
        /// <param name="directoryPath">The path for which to retrieve the parent directory</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the parent directory</returns>
        Task<string> GetParentDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Checks if the path is directory
        /// </summary>
        /// <param name="path">Path for check</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether passed path is a directory</returns>
        Task<bool> IsDirectoryAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the path</returns>
        Task<string> MapPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Reads the contents of the file into a byte array
        /// </summary>
        /// <param name="filePath">The file for reading</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains a byte array containing the contents of the file</returns>
        Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading</param>
        /// <param name="encoding">The encoding applied to the contents of the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the read text from a file by the passed path</returns>
        Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to
        /// </summary>
        /// <param name="path">The file for which to set the date and time information</param>
        /// <param name="lastWriteTimeUtc">A System.DateTime containing the value to set for the last write date and time of path. This value is expressed in UTC time</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the date and time is set</returns>
        Task SetLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Writes the specified byte array to the file
        /// </summary>
        /// <param name="filePath">The file to write to</param>
        /// <param name="bytes">The bytes to write to the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that byte array is written to the file</returns>
        Task WriteAllBytesAsync(string filePath, byte[] bytes, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates a new file, writes the specified string to the file using the specified encoding,
        /// and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to</param>
        /// <param name="contents">The string to write to the file</param>
        /// <param name="encoding">The encoding to apply to the string</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that passed text is written to a file by the passed path</returns>
        Task WriteAllTextAsync(string path, string contents, Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}