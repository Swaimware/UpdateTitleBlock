# UpdateTitleBlock

Title:       AutoCAD Title Block Update

Author:      Kurt Swaim
Language:    C# .NET 4.7

Platform:    Windows 10

Technology:  ObjectARX

Level:       Intermediate

Description: Begins with introduction on how to create AutoCAD plug-in.  Then an in depth discussion on how to update title blocks on AutoCAD drawings.

Section      AutoCAD, Autodesk, ObjectARX

SubSection   C#, plug-in, .NET

License:     CPOL

Introduction

This article begins with an explanation of how to implement a plug-in for AutoCAD. This will be accomplished by using Autodesk’s ObjectARX for C# .NET. This initial explanation will include an introduction to passing AutoCAD commands and receiving messages. This will be followed with how to update named title blocks that have pre-defined attributes on the currently open drawing. Finally we will side load all drawing files from a specified directory and update the title block of each. 

To download the ObjectARX SDK for.NET and find more information please start here. Make sure you download the SDK that matches your AutoCAD version. Check out the PDF of AutoCAD .NET developer’s Guide. The PDF is from an older version, to find newer online documentation look for the document section at the AutoCAD Developer Center. In addition there are two blogs at Through The Interface and ADN Dev Blog. 

Background

Part of my responsibility as a control engineer is to create electrical drawing sets for various manufacturing equipment. For over 10 years I had the benefit of using AutoCAD Electrical for this task. The efficiency, ease of use, decreased human error, and power of AutoCAD Electrical became obvious when I recently changed jobs. Processes that took seconds (e.g. updating title blocks on drawings) now took hours and were error prone. I decided to start writing code to replace some of those lost features, starting with automatic title block updates. Because I could find little help beyond Autodesk I decided to give back to the community. 
The Project

The plug-in for AutoCAD utilizes a C# .NET 4.7 class library project. I'm using 4.7 because I am targeting AutoCAD 2018 and windows 10. The SDK comes with several DLL files that can be referenced and used in your class library. Three of the DLL files (i.e. AcCoreMgd.dll, AcDbMgd.dll, and AcMgd.dll) are the most common and are referenced in this library project.

The below shows the beginning of the Update Title Block class project. The using statements utilizes the referenced DLL files I described above. The field variables shown at the beginning of the class allow for hard coded values. In my production code I utilize SQL database code to pull this information into my command methods.

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace UpdateTitleBlock
{
    public class Revision
    {
	    private string BlockName = "Your Block Name";
        private Dictionary<string, string> Attributes = new Dictionary<string, string>
            {
            {"AttributeName1", "AttributeValue1"},
            {"AttributeName2", "AttributeValue2"},
            {"AttributeName3", "AttributeValue3"}
            };
        string path = "C:\\Drawings\\";

Using a Command

In the project properties Debug tab add AutoCADs exe file into the Start Action section. Now when you press Start button to test your code the AutoCAD program will launch. Once AutoCAD is up type NETLOAD into its command prompt. Then point to your UpdateTitleBlock.dll file inside your bin/debug folder. Now your plug-in is loaded and you can use your new commands. Note that you can debug as normal with break points and the like to solve any issues that may arrise. 

The below code shows a simple series of commands. Notice this command is marked with [CommandMethod("CMD")]. This declaration both marks the method as a new AutoCAD command but also specifies the keyword to run the command. This Command Method uses CMD as the keyword. Once the DLL is loaded, as described above, type CMD in AutoCAD command prompt which will run the command. 

This command is actually three commands in one (i.e. Regenerate, Zoom Extents, and then Quick Save). The commands are controlled through AutoCADs Editor from the active drawing. The Editor object allows your code to interface with AutoCADs command prompt. This objects can give commands directly (like this one does), give messages, and obtain input from the users. The ed.command() parameters accepts an array, with each indices causing an enter key. For example, the zoom command puts ZOOM in AutoCAD's command prompt followed by enter. The second element (E) selects the extents option of ZOOM. 

        [CommandMethod("CMD")]
        public void Commands()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.Command("REGEN");
            ed.Command("ZOOM", "E");
            ed.Command("QSAVE");
        }

Updating the Title Block

The below allows the active drawings title block attributes to be updated based on a hard coded Dictionary. The first step is to grab the database from the active drawing. This database stores all of the information of the drawing including the blocks. The title block is just one of many blocks that might be included in a drawing. Attributes is variable text in blocks. This text is what we are updating. 

The database is then used to start a transaction. Like all transactions no changes take effect till commit is used. Within the transaction we first grab the block table from the database. The block table is used to grab the block table record of the model space. The block table record stores the block references. This is a reference to the various blocks of the drawing. Each reference has an attribute collection. Finally the attribute collection holds the attributes we are trying to change.

Now that we have the block table record we can iterate through looking at each ID. Each ID is used to create the corresponding block reference. Once we have the block reference we can compare its name against the one we are searching for (i.e. hard coded with BlockName). Once we find a block name that matches the title block then the attribute collection is iterated through. Each time an attribute with a tag name matches a dictionary key we change its value with the corresponding dictionary value. 

        [CommandMethod("REV")]
        public void EditBlock()
        {
		        // Get the database of the active drawing
            var acDb = HostApplicationServices.WorkingDatabase;
            // Start a transaction
            using (var acTrans = acDb.TransactionManager.StartTransaction())
            {
			           // Get the block table from the database
                var acBlockTable = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (acBlockTable == null) return;
                // Get the block table record from the block table
                var acBlockTableRecord = acTrans.GetObject(acBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                if (acBlockTableRecord == null) return;
                // Search through the block table record for the title block name
                foreach (var blkId in acBlockTableRecord)
                {
					          // Get block reference from block table record id
                    var acBlock = acTrans.GetObject(blkId, OpenMode.ForWrite) as BlockReference;
                    if (acBlock == null) continue;
                    // Compare block reference name from title block name being searched for
                    if (!acBlock.Name.Equals(BlockName, StringComparison.CurrentCultureIgnoreCase)) continue;
                    // Search through the attribute collection of the title block
                    foreach (ObjectId attId in acBlock.AttributeCollection)
                    {
					              // Get the attribute reference
                        var acAtt = acTrans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (acAtt == null) continue;
                        // Check if the attribute tag name matches a key in the dictionary
                        if (!Attributes.ContainsKey(acAtt.Tag)) continue;
                        // When tag name matches change its text value to the dictionary value
                        acAtt.UpgradeOpen();
                        acAtt.TextString = Attributes[acAtt.Tag];
                    }
                }
                acTrans.Commit();
            }
        }

Updating All the Title Blocks From a directory

This command side loads all of the drawings located in the directory hard coded with the field string 'path'. As each drawing is side loaded the title block is updated then saved. Side load means the drawing is loaded into memory and never shows up on the AutoCAD user interface.

If the directory of drawings contain a large number of drawings this command could be long running. For that reason the command utilizes a task to send the process on a separate thread. The first thing the command does is stores the database of the current drawing. This database will be restored at the end of this command. Note that you should not have any of the drawings you are trying to side load open in AutoCAD.

This command simply uses the same title block update code from above iterated over an array of files. First the DirectoryInfo is collected using the path. Next this is used to collect an array of FileInfo which contains all of the drawings in the path directory. As each file is iterated a new database is used to read the drawing into the database in memory. Then the memory based database is used as before to start a transaction and update the title block as was done above.

        [CommandMethod("REVALL")]
        public void RevAllDwgFiles()
        {
		         // Command ran with a task because of long life
            Task t = Task.Run(() =>
            {     
			        	// Temp save active drawings database to be recovered at end of command
                Database currentDatabase = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                try
                {      
				          	// Get array of drawing names
                    DirectoryInfo d = new DirectoryInfo(path);
                    FileInfo[] Files = d.GetFiles("*.dwg");
				          	// Iterate through each file
                    foreach (FileInfo file in Files)
                    {
				             		// Create full path of drawing
                        var fileName = Path.GetFileName(file.FullName);
                        string dwgFlpath = path + fileName;
					            	// Create new database to be used in side load of drawing
                        using (Database acDb = new Database(false, true))
                        {
						              	// Side load drawing into database
                            acDb.ReadDwgFile(dwgFlpath, FileOpenMode.OpenForReadAndAllShare, false, null);
                            // Assign database to the working database
                            HostApplicationServices.WorkingDatabase = acDb;
						              	// Start a transaction
                            using (Transaction acTrans = acDb.TransactionManager.StartTransaction())
                            {
							                	// Get the block table from the database
                                var acBlockTable = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                                if (acBlockTable == null) return;
							                	// Get the block table record from the block table
                                var acBlockTableRecord = acTrans.GetObject(acBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                                if (acBlockTableRecord == null) return;
							                	// Search through the block table record for the title block name
                                foreach (var blkId in acBlockTableRecord)
                                {
								                  	// Get block reference from block table record id
                                    var acBlock = acTrans.GetObject(blkId, OpenMode.ForWrite) as BlockReference;
                                    if (acBlock == null) continue;
								                  	// Compare block reference name from title block name being searched for
                                    if (!acBlock.Name.Equals(BlockName, StringComparison.CurrentCultureIgnoreCase)) continue;
									                  // Search through the attribute collection of the title block
                                    foreach (ObjectId attId in acBlock.AttributeCollection)
                                    {
									                    	// Get the attribute reference
                                        var acAtt = acTrans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                                        if (acAtt == null) continue;
									                    	// Check if the attribute tag name matches a key in the dictionary
                                        if (!Attributes.ContainsKey(acAtt.Tag)) continue;
										                    // When tag name matces change its text value to the dictionary value
                                        acAtt.UpgradeOpen();
                                        acAtt.TextString = Attributes[acAtt.Tag];
                                    }
                                }
                                acTrans.Commit();
                            }
                            acDb.SaveAs(dwgFlpath, DwgVersion.AC1027);
                            
                            HostApplicationServices.WorkingDatabase = currentDatabase;
                        }
                    }
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("All files processed");
                }
                catch (System.Exception ex)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
                }
            });
            t.Wait();            
        }

History

June 2018 - Create plugin commands

January 2020 - Create article
