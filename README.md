# HzDTextureExplorer
 Texture Explorer for Horizon Zero Dawn files.
 Can view Textures in a HzD core file, export as dds, and reimport dds
 
 Uses Pfim and ImageSharp

 ![Screenshot of tool](/Screenshots/tool.png?raw=true)
 
 # Features
 * Open HzD core files containing textures and preview them.
     * You can also drop a core file on the program to open it
 * Export Textures from the file to dds on disk
 * Replace the textures in the .core.stream file with your own modded dds file.
 * Support for viewing and exporting UITextures
 
 # Usage
 * Open a .core file with *Open*. For everything but UITextures, you also need a .core.stream file in the same folder.
 * Use Export or Export All to export the select or all textures to the same folder as the .core file (as dds files).
 * Use Replace to replace the selected texture with a dds file from disk.
     * The new file must have the same resolution, format and at least the same number of mipmaps as the original file.
	 * This updates the existing .core.stream file
 * Use Replace all to replace all textures with dds files with matching names.