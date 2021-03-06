# HzDTextureExplorer
 Texture Explorer for Horizon Zero Dawn files.
 Can view Textures in a HzD core file, export as dds, and reimport dds
 
 Uses Pfim and ImageSharp

 ![Screenshot of tool](/Screenshots/tool.png?raw=true)
 
 # Features
 * Open HzD core files containing textures and preview them.
     * You can also drop a core file on the program to open it
 * Export textures to dds or tga
 * Replace textures in the .core.stream file with your own modded dds file.
 * Support for viewing and exporting UITextures (replacing not supported yet)
 
 # Usage
 * Open a .core file with *Open*. For everything but UITextures, you also need a .core.stream file in the same folder.
 * Use Export Selected to export the selected texture. Select between dds and tga format.
 * Use Export All to export all textures. The filename provided in the dialog is used as a base.
 * Use Replace to replace the selected texture with a dds file from disk.
     * The new file must have the same resolution, format and at least the same number of mipmaps as the original file.
	 * This updates the existing .core.stream file
 * Use Replace all to replace all textures with dds files with matching names.

# Note on Photoshop
* When loading the dds in photoshop, it's best to uncheck "load mipmaps", and check "Load alpha as channel ...". This ensures the texture is visible.
* When exporting, make sure to match the format (see info window of tool), and remember to check "generate mipmaps"
