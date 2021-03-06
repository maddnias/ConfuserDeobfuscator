/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using dnlib.Utils;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.W32Resources {
	/// <summary>
	/// Win32 resources base class
	/// </summary>
	public abstract class Win32Resources : IDisposable {
		/// <summary>
		/// Gets/sets the root directory
		/// </summary>
		public abstract ResourceDirectory Root { get; set; }

		/// <summary>
		/// Finds a <see cref="ResourceDirectory"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns>The <see cref="ResourceDirectory"/> or <c>null</c> if none found</returns>
		public ResourceDirectory Find(ResourceName type) {
			var dir = Root;
			if (dir == null)
				return null;
			return dir.FindDirectory(type);
		}

		/// <summary>
		/// Finds a <see cref="ResourceDirectory"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <returns>The <see cref="ResourceDirectory"/> or <c>null</c> if none found</returns>
		public ResourceDirectory Find(ResourceName type, ResourceName name) {
			var dir = Find(type);
			if (dir == null)
				return null;
			return dir.FindDirectory(name);
		}

		/// <summary>
		/// Finds a <see cref="ResourceData"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <param name="langId">Language ID</param>
		/// <returns>The <see cref="ResourceData"/> or <c>null</c> if none found</returns>
		public ResourceData Find(ResourceName type, ResourceName name, ResourceName langId) {
			var dir = Find(type, name);
			if (dir == null)
				return null;
			return dir.FindData(langId);
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			var root = Root;
			if (root != null)
				root.Dispose();
			Root = null;
		}
	}

	/// <summary>
	/// Win32 resources class created by the user
	/// </summary>
	public class Win32ResourcesUser : Win32Resources {
		ResourceDirectory root = new ResourceDirectoryUser(new ResourceName("root"));

		/// <inheritdoc/>
		public override ResourceDirectory Root {
			get { return root; }
			set { root = value; }
		}
	}

	/// <summary>
	/// Win32 resources class created from a PE file
	/// </summary>
	public sealed class Win32ResourcesPE : Win32Resources {
		/// <summary>
		/// Converts data RVAs to file offsets in <see cref="dataReader"/>
		/// </summary>
		IRvaFileOffsetConverter rvaConverter;

		/// <summary>
		/// This reader only reads the raw data. The data RVA is found in the data header and
		/// it's first converted to a file offset using <see cref="rvaConverter"/>. This file
		/// offset is where we'll read from using this reader.
		/// </summary>
		IImageStream dataReader;

		/// <summary>
		/// This reader only reads the directory entries and data headers. The data is read
		/// by <see cref="dataReader"/>
		/// </summary>
		IBinaryReader rsrcReader;

		UserValue<ResourceDirectory> root;

		/// <inheritdoc/>
		public override ResourceDirectory Root {
			get { return root.Value; }
			set { root.Value = value; }
		}

		/// <summary>
		/// Gets the resource reader
		/// </summary>
		public IBinaryReader ResourceReader {
			get { return rsrcReader; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rvaConverter"><see cref="RVA"/>/<see cref="FileOffset"/> converter</param>
		/// <param name="dataReader">Data reader (it's used after converting an <see cref="RVA"/>
		/// to a <see cref="FileOffset"/>). This instance owns the reader.</param>
		/// <param name="rsrcReader">Reader for the whole Win32 resources section (usually
		/// the .rsrc section). It's used to read <see cref="ResourceDirectory"/>'s and
		/// <see cref="ResourceData"/>'s but not the actual data blob. This instance owns the
		/// reader.</param>
		public Win32ResourcesPE(IRvaFileOffsetConverter rvaConverter, IImageStream dataReader, IBinaryReader rsrcReader) {
			if (dataReader == rsrcReader)
				rsrcReader = dataReader.Clone();	// Must not be the same readers
			this.rvaConverter = rvaConverter;
			this.dataReader = dataReader;
			this.rsrcReader = rsrcReader;
			Initialize();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		public Win32ResourcesPE(IPEImage peImage)
			: this(peImage, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="rsrcReader">Reader for the whole Win32 resources section (usually
		/// the .rsrc section) or <c>null</c> if we should create one from the resource data
		/// directory in the optional header. This instance owns the reader.</param>
		public Win32ResourcesPE(IPEImage peImage, IBinaryReader rsrcReader) {
			this.rvaConverter = peImage;
			this.dataReader = peImage.CreateFullStream();
			if (rsrcReader != null)
				this.rsrcReader = rsrcReader;
			else {
				var dataDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[2];
				if (dataDir.VirtualAddress != 0 && dataDir.Size != 0)
					this.rsrcReader = peImage.CreateStream(dataDir.VirtualAddress, dataDir.Size);
				else
					this.rsrcReader = MemoryImageStream.CreateEmpty();
			}
			Initialize();
		}

		void Initialize() {
			root.ReadOriginalValue = () => {
				if (rsrcReader == null)
					return null;	// It's disposed
				long oldPos = rsrcReader.Position;
				rsrcReader.Position = 0;
				var dir = new ResourceDirectoryPE(0, new ResourceName("root"), this, rsrcReader);
				rsrcReader.Position = oldPos;
				return dir;
			};
		}

		/// <summary>
		/// Creates a new data reader
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="size">Size of data</param>
		/// <returns>A new <see cref="IBinaryReader"/> for this data</returns>
		public IBinaryReader CreateDataReader(RVA rva, uint size) {
			var reader = dataReader.Create(rvaConverter.ToFileOffset(rva), size);
			if (reader.Length == size)
				return reader;
			reader.Dispose();
			return MemoryImageStream.CreateEmpty();
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (!disposing)
				return;
			if (dataReader != null)
				dataReader.Dispose();
			if (rsrcReader != null)
				rsrcReader.Dispose();
			dataReader = null;
			rsrcReader = null;
			base.Dispose(disposing);
		}
	}
}
