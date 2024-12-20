﻿using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    class FileSystemApi : IFileSystemApi
    {
        private IpfsClient ipfs;

        internal FileSystemApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IFileSystemNode> AddFileAsync(string path, AddFileOptions options = null, CancellationToken cancel = default)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var node = await AddAsync(stream, Path.GetFileName(path), options, cancel).ConfigureAwait(false);
                return node;
            }
        }

        public Task<IFileSystemNode> AddTextAsync(string text, AddFileOptions options = null, CancellationToken cancel = default)
        {
            return AddAsync(new MemoryStream(Encoding.UTF8.GetBytes(text), false), "", options, cancel);
        }

        public async Task<IFileSystemNode> AddAsync(Stream stream, string name = "", AddFileOptions options = null, CancellationToken cancel = default)
        {
            if (options == null)
                options = new AddFileOptions();

            var opts = new List<string>();
            if (!options.Pin)
                opts.Add("pin=false");
            if (options.Wrap)
                opts.Add("wrap-with-directory=true");
            if (options.RawLeaves)
                opts.Add("raw-leaves=true");
            if (options.OnlyHash)
                opts.Add("only-hash=true");
            if (options.Trickle)
                opts.Add("trickle=true");
            if (options.Progress != null)
                opts.Add("progress=true");
            if (options.Hash != MultiHash.DefaultAlgorithmName)
                opts.Add($"hash=${options.Hash}");
            if (options.Encoding != MultiBase.DefaultAlgorithmName)
                opts.Add($"cid-base=${options.Encoding}");
            if (!string.IsNullOrWhiteSpace(options.ProtectionKey))
                opts.Add($"protect={options.ProtectionKey}");

            opts.Add($"chunker=size-{options.ChunkSize}");

            // UnityEngine.Debug.Log($"--> Upload: {name}");
            using var response = await ipfs.Upload2Async("add", cancel, stream, name, opts.ToArray()).ConfigureAwait(false);
            // UnityEngine.Debug.Log($"--- Upload: {name}, getting response...");

            // The result is a stream of LDJSON objects.
            // See https://github.com/ipfs/go-ipfs/issues/4852
            FileSystemNode fsn = null;
            FileSystemNode previousFsn = null;
            using (var sr = new StreamReader(response))
            using (var jr = new JsonTextReader(sr) { SupportMultipleContent = true })
            {
                while (jr.Read())
                {
                    // UnityEngine.Debug.Log($"--- Upload: {name}, getting response item");
                    var r = await JObject.LoadAsync(jr, cancel).ConfigureAwait(false);

                    // If a progress report.
                    if (r.ContainsKey("Bytes"))
                    {
                        options.Progress?.Report(new TransferProgress
                        {
                            Name = (string)r["Name"],
                            Bytes = (ulong)r["Bytes"]
                        });
                    }

                    // Else must be an added file.
                    else
                    {
                        fsn = new FileSystemNode
                        {
                            Id = (string)r["Hash"],
                            Size = long.Parse((string)r["Size"]),
                            IsDirectory = false,
                            Name = name,
                            Links = Enumerable.Empty<FileSystemLink>(),
                        };

                        // Wrapping/Chaining: Previous node will link to the current node.
                        if (previousFsn != null)
                            fsn.Links = fsn.Links.Append(previousFsn.ToLink());
                    }

                    previousFsn = fsn;
                }
            }

            // UnityEngine.Debug.Log($"<-- Upload: {name}, {fsn.Id}");

            fsn.IsDirectory = options.Wrap;
            return fsn;
        }

        public async Task<IFileSystemNode> AddDirectoryAsync(string path, bool recursive = true, AddFileOptions options = null, CancellationToken cancel = default)
        {
            if (options == null)
                options = new AddFileOptions();
            options.Wrap = false;
            var opts = new List<string>();
            if (!options.Pin)
                opts.Add("pin=false");
            if (options.Wrap)
                opts.Add("wrap-with-directory=true");
            if (options.RawLeaves)
                opts.Add("raw-leaves=true");
            if (options.OnlyHash)
                opts.Add("only-hash=true");
            if (options.Trickle)
                opts.Add("trickle=true");
            if (options.Progress != null)
                opts.Add("progress=true");
            if (options.Hash != MultiHash.DefaultAlgorithmName)
                opts.Add($"hash=${options.Hash}");
            if (options.Encoding != MultiBase.DefaultAlgorithmName)
                opts.Add($"cid-base=${options.Encoding}");
            if (!string.IsNullOrWhiteSpace(options.ProtectionKey))
                opts.Add($"protect={options.ProtectionKey}");

            opts.Add($"chunker=size-{options.ChunkSize}");


            string rootPath = Path.GetDirectoryName(path);
            string rootDir = Path.GetFileName(path);

            if (!Directory.Exists(rootPath))
                throw new ArgumentException($"{path} is not an accessible directory");

            // Depth-first, only files. Let Kubo rebuild the hierarchy.
            List<string> fileCollection = ListDirectory(path, recursive, false);

            if (fileCollection.Count == 0) fileCollection.Add(path);

            for (int i = 0; i < fileCollection.Count; i++)
            {
                string file = fileCollection[i];
                fileCollection[i] = file[(rootPath.Length+1)..];
            }

            using var response = await ipfs.UploadMultipleAsync("add", cancel, rootPath, fileCollection, opts.ToArray()).ConfigureAwait(false);

            FileSystemNode fsn = null;
            using (var sr = new StreamReader(response))
            using (var jr = new JsonTextReader(sr) { SupportMultipleContent = true })
            {
                while (jr.Read())
                {
                    // UnityEngine.Debug.Log($"--- Upload: {name}, getting response item");
                    var r = await JObject.LoadAsync(jr, cancel).ConfigureAwait(false);

                    // If a progress report.
                    if (r.ContainsKey("Bytes"))
                    {
                        options.Progress?.Report(new TransferProgress
                        {
                            Name = (string)r["Name"],
                            Bytes = (ulong)r["Bytes"]
                        });
                    }

                    // Else must be an added file.
                    else
                    {
                        fsn = new FileSystemNode
                        {
                            Id = (string)r["Hash"],
                            Size = long.Parse((string)r["Size"]),
                            IsDirectory = false,
                            Name = (string)r["Name"],
                            Links = Enumerable.Empty<FileSystemLink>(),
                        };
                        // UnityEngine.Debug.Log($"Name: {fsn.Name}, Hash: {fsn.Id}, Size: {fsn.Size}");
                    }
                }
            }

            // Last FSN is supposed to be the root directory of the submitted hierarchy.
            // Returned FSN data is incomplete, ask Kubo about the result.
            return await ipfs.FileSystem.ListAsync(fsn.Id, cancel);
        }

        private List<string> ListDirectory(string dir, bool recursive, bool self)
        {
            List<string> list = new();

            if(recursive)
            {
                foreach (string subdir in Directory.EnumerateDirectories(dir))
                    list.AddRange(ListDirectory(subdir, recursive, false));
            }

            List<string> files = Directory.EnumerateFiles(dir).ToList();
            files.Sort();
            list.AddRange(files);

            if(self) list.Add(dir);

            return list;
        }

        public async Task<FileSystemNode> CreateDirectoryAsync(IEnumerable<IFileSystemLink> links, bool pin = true, CancellationToken cancel = default)
        {
            List<JToken> linkList = new();

            void AddFileLink(IFileSystemLink node)
            {
                JToken newLink = JToken.Parse(@"{ ""Hash"": { ""/"": """" }, ""Name"": """", ""Tsize"": 0 }");
                newLink["Hash"]["/"] = node.Id.ToString();
                newLink["Name"] = node.Name;
                newLink["Tsize"] = node.Size;

                linkList.Add(newLink);
            }

            JToken dir = JToken.Parse(@"{ ""Data"": { ""/"": { ""bytes"": ""CAE"" } }, ""Links"": [] }");
            foreach (var link in links)
                AddFileLink(link);

            // Sorting? I've checked. kubo sorts the links on its own.
            dir["Links"] = JToken.FromObject(linkList);
            var id = await ipfs.Dag.PutAsync(dir, "dag-pb", pin: pin, cancel: cancel).ConfigureAwait(false);

            // HACK: Retrieve the resulting serialized DAG node rather than serializing it itself.
            byte[] rawDAG = await ipfs.Block.GetAsync(id).ConfigureAwait(false);
            long totalBytes = rawDAG.LongLength;

            foreach (IFileSystemLink link in links)
                totalBytes += link.Size;

            return new FileSystemNode
            {
                Id = id,
                Links = links,
                Size = totalBytes,
                IsDirectory = true
            };
        }


        /// <summary>
        ///   Reads the content of an existing IPFS file as text.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   The contents of the <paramref name="path"/> as a <see cref="string"/>.
        /// </returns>
        public async Task<String> ReadAllTextAsync(string path, CancellationToken cancel = default)
        {
            using (var data = await ReadFileAsync(path, cancel).ConfigureAwait(false))
            using (var text = new StreamReader(data))
            {
                return await text.ReadToEndAsync();
            }
        }


        /// <summary>
        ///   Opens an existing IPFS file for reading.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A <see cref="Stream"/> to the file contents.
        /// </returns>
        public Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default)
        {
            return ipfs.PostDownloadAsync("cat", cancel, path);
        }

        public Task<Stream> ReadFileAsync(string path, long offset, long length = 0, CancellationToken cancel = default)
        {
            // https://github.com/ipfs/go-ipfs/issues/5380
            if (offset > int.MaxValue)
                throw new NotSupportedException("Only int offsets are currently supported.");
            if (length > int.MaxValue)
                throw new NotSupportedException("Only int lengths are currently supported.");

            if (length == 0)
                length = int.MaxValue; // go-ipfs only accepts int lengths
            return ipfs.PostDownloadAsync("cat", cancel, path,
                $"offset={offset}",
                $"length={length}");
        }

        /// <inheritdoc cref="ListAsync"/>
        public Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default)
        {
            return ListAsync(path, cancel);
        }

        /// <summary>
        ///   Get information about the directory.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing directory, such as "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task. When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns></returns>
        public async Task<IFileSystemNode> ListAsync(string path, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("ls", cancel, path).ConfigureAwait(false);
            var r = JObject.Parse(json);
            var o = (JObject)r["Objects"]?[0];

            var node = new FileSystemNode()
            {
                Id = (string)o["Hash"],
                IsDirectory = true,
                Links = Array.Empty<FileSystemLink>(),
            };

            if (o["Links"] is JArray links)
            {
                node.Links = links
                    .Select(l => new FileSystemLink()
                    {
                        Name = (string)l["Name"],
                        Id = (string)l["Hash"],
                        Size = (long)l["Size"],
                    })
                    .ToArray();
            }

            return node;
        }

        public Task<Stream> GetAsync(string path, bool compress = false, CancellationToken cancel = default)
        {
            return ipfs.PostDownloadAsync("get", cancel, path, $"compress={compress}");
        }
    }
}
