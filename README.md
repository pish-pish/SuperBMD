# SuperBMD
A library to import and export various 3D model formats into the Binary Model (BMD) format.

This API uses the Open Asset Import Library (AssImp). A list of supported model formats can be found [here](http://assimp.org/main_features_formats.html).

This is a fork of SuperBMD that supports generating triangle strips (a more space-efficient way to represent faces in a model) 
and extracting and applying BMD material data as JSON, maintained by Yoshi2 (RenolY2 on github). 
Please report any issues you find here: https://github.com/RenolY2/SuperBMD/issues

# Usage

The UnitTest program can be used to convert models to the BMD format and BMD models to the DAE format.

To convert a model, drag it onto the executable or run the program via command line:

`SuperBMD.exe <model path>`

If model path is a BMD, it will be converted to DAE, its textures will be dumped as .PNG and 
its material data will be dumped as JSON in the same folder as the DAE and the textures.

If model path is a model but not BMD, it will be converted to BMD. By default triangle strips 
are generated for static meshes but not rigged meshes. See below for more options on this.

# Full usage
`SuperBMD.exe <input path> [output path] [--mat <material path>] [--tristrip (all/static/none)]`

The last three arguments are optional. If output path is left out it is created from the input 
path by replacing the extension either with `.bmd` or `.dae`, depending on input. If material 
path is left out, it's created from the input path (when converting to BMD) or output path 
(when converting BMD to DAE) by replacing the extension with `_mat.json`

If input path is a BMD/BDL and output path is a BMD, the model will be loaded, materials are 
applied from <material path> if supplied and then written to output path. 
  
If input path is BMD then the extracted DAE is written to output path. Textures are dumped to 
the same directory as the DAE. If <material path> is not set, material data is dumped to the 
  same directory, otherwise the material data is dumped to the material path.

If input path is not a BMD, it is converted into a BMD and written to output path. if material 
path is set, materials are loaded from that path and applied to the model. If tristrip option 
is set to all, triangle strips are always generated. If set to static, they are generated for 
static meshes (without rigs) only. If set to none, no triangle strips are generated. Triangle 
strips make the file size of the model smaller but can introduce issues in some rigged models 
(possible way to avoid issues: Never set more than 3 weights per vertex in a face?).

## Notes
### Modeling
* When exporting a model for conversion to BMD, rotate the model about the X axis by -90 degrees. 
Most modeling programs define the Z axis as the up axis, but Nintendo games use the Y axis instead. 
Rotating the model ensures that the model is not sideways when imported into a game.

### Skinning
* SuperBMD supports both skinned and unskinned models.
* For skinned meshes, <b>make the root of the model's skeleton the child of a dummy object called 
  `skeleton_root`.</b> SuperBMD uses the name of this dummy object to find the root of the skeleton 
  so that it can process it.
* If a `skeleton_root` object is not found, then the model will be imported with a single root bone. 
This is recommended for models intended for maps.

### Vertex Colors
* SuperBMD supports vertex colors.

### Textures
* SuperBMD supports models that have no textures. These models will appear white when imported into a game.
* It is recommended that the model's textures be in the same directory as the model being converted. <b>
If SuperBMD cannot find the model's textures, it will use a black and white checkerboard image instead.</b>
* Textures must be in either BMP, JPG, or PNG format. TGA is currently not supported.

### Materials
* This fork of SuperBMD allows dumping and inserting materials as JSON and it allows additional 
textures to be loaded without having to apply them in a 3D modelling program. Look at Full Usage 
above on how to extract and apply material files. While Drag&Drop works, writing bat files is recommended.

* The json file will contain one or more materials from the BMD file. When applying materials, the material 
names are exactly the name of the materials of the model when exported from a 3D modelling program. 
You do not have to cover every material, SuperBMD will generate material data for the rest. You can 
also use a default preset for every material in the model by naming the material in the JSON ``__MatDefault``.

* The ``TextureRefs`` section contains the names of the textures used by the material. Usually the 
first texture is the main texture, the rest are used for graphics effects. When using a ``__MatDefault`` 
material it can be useful to leave the first texture name as ``null`` so each material will retain the 
texture name it originally had.

* Additional textures can be loaded by adding their names in the ``TextureRefs`` section of a material. 
The program will search for these textures in the folder of the input file by appending an extension to 
the name in this order: ``.png``, ``.jpg``, ``.tga``, ``.bmp``. (Example: If there are two textures 
``Texture.png`` and ``Texture.bmp``, the PNG texture will be loaded rather than the BMP texture)

* You can modify materials if you know what you are doing. Look on the internet for info on TEV and 
check the source code of SuperBMD for possible values for some of the enums (When viewing the source 
code in VS or other good IDEs, check ``SuperBMD/source/Materials/Material.cs``). A recap of all enums 
and useful info about materials will be available here https://github.com/RenolY2/SuperBMD/wiki when 
it is added.

## Attribution
This project uses a number of external libraries or parts of them in the code which will be listed here 
together with their license if necessary:

* BrawlLib: https://github.com/libertyernie/brawltools
<br>

* TGA reader by Dmitry Brant: http://dmitrybrant.com
<br>
If you acquired the compiled release of SuperBMD, the following licenses also apply:

* Assimp: (Assimp32.dll, Assimp64.dll, AssimpNet.dll)
```
 Copyright (c) 2006-2015 assimp team
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    Neither the name of the assimp team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```
<br>

* Json.NET: (Newtonsoft.Json.dll)
```


The MIT License (MIT)

Copyright (c) 2007 James Newton-King

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```
