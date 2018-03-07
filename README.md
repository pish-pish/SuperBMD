# SuperBMD
A library to import and export various 3D model formats into the Binary Model (BMD) format.

This API uses the Open Asset Import Library (AssImp). A list of supported model formats can be found [here](http://assimp.org/main_features_formats.html).

# Usage

The UnitTest program can be used to convert models to the BMD format and BMD models to the DAE format.

To convert a model, drag it onto the executable or run the program via command line:

`SuperBMD.exe <model path>`

If model path is a BMD, it will be converted to DAE, its textures will be dumped as .BMP and its material data will be dumped as JSON in the same folder as the DAE and the textures.

If model path is a model but not BMD, it will be converted to BMD. By default triangle strips are generated for static meshes but not rigged meshes. See below for more options on this.

# Full usage
`SuperBMD.exe <input path> [output path] [--mat <material path>] [--tristrip (all/static/none)]`

The last three arguments are optional. If output path is left out it is created from the input path by replacing the extension either with `.bmd` or `.dae`, depending on input. If material path is left out, it's created from the input path by replacing the extension with `_mat.json`

If input path is a BMD/BDL and output path is a BMD, the model will be loaded, materials are applied from <material path> if supplied and then written to output path. 
  
If input path is BMD then the extracted DAE is written to output path. Textures are dumped to the same directory as the DAE. If <material path> is not set, material data is dumped to the same directory, otherwise the material data is dumped to the material path.

If input path is not a BMD, it is converted into a BMD and written to output path. if material path is set, materials are loaded from that path and applied to the model. If tristrip option is set to all, triangle strips are always generated. If set to static, they are generated for static meshes (without rigs) only. If set to none, no triangle strips are generated. Triangle strips make the file size of the model smaller but can introduce issues in some rigged models (possible way to avoid issues: Never set more than 3 weights per vertex in a face?).

## Notes
### Modeling
* When exporting a model for conversion to BMD, rotate the model about the X axis by -90 degrees. Most modeling programs define the Z axis as the up axis, but Nintendo games use the Y axis instead. Rotating the model ensures that the model is not sideways when imported into a game.

### Skinning
* SuperBMD supports both skinned and unskinned models.
* For skinned meshes, <b>make the root of the model's skeleton the child of a dummy object called `skeleton_root`.</b> SuperBMD uses the name of this dummy object to find the root of the skeleton so that it can process it.
* If a `skeleton_root` object is not found, then the model will be imported with a single root bone. This is recommended for models intended for maps.

### Vertex Colors
* SuperBMD supports vertex colors.

### Textures
* SuperBMD supports models that have no textures. These models will appear white when imported into a game.
* It is recommended that the model's textures be in the same directory as the model being converted. <b>If SuperBMD cannot find the model's textures, it will use a black and white checkerboard image instead.</b>
* Textures must be in either BMP, JPG, or PNG format. TGA is currently not supported.

### Materials
* This fork of SuperBMD allows dumping and inserting materials as JSON and it allows additional textures to be loaded without having to apply them in a 3D modelling program. Look at Full Usage above on how to extract and apply material files. While Drag&Drop works, writing bat files is recommended.
* The json file will contain one or more materials from the BMD file. When applying materials, the material names are exactly the name of the materials of the model when exported from a 3D modelling program. You do not have to cover every material, SuperBMD will generate material data for the rest. You can also use a default preset for every material in the model by naming the material in the JSON ``__MatDefault``.
* The ``TextureRefs`` section contains the names of the textures used by the material. Usually the first texture is the main texture, the rest are used for graphics effects. When using a ``__MatDefault`` material it can be useful to leave the first texture name as ``null`` so each material will retain the texture name it originally had.
* Additional textures can be loaded by adding their names in the ``TextureRefs`` section of a material. The program will add ``.bmp`` to their name (so textures dumped with SuperBMD work) and look for the texture in the same directory as the input model.
* You can modify materials if you know what you are doing. Look on the internet for info on TEV and check the source code of SuperBMD for possible values for some of the enums (When viewing the source code in VS or other good IDEs, check ``SuperBMD/source/Materials/Material.cs``). A recap of all enums and useful info about materials will be available here https://github.com/RenolY2/SuperBMD/wiki when it is added.
