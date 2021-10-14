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
        string path = "C:\\Users\\";

        [CommandMethod("CMD")]
        public void Commands()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.Command("REGEN");
            ed.Command("ZOOM", "E");
            ed.Command("QSAVE");
        }

        [CommandMethod("REV")]
        public void EditBlock()
        {
            var acDb = HostApplicationServices.WorkingDatabase;

            using (var acTrans = acDb.TransactionManager.StartTransaction())
            {
                var acBlockTable = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (acBlockTable == null) return;

                var acBlockTableRecord = acTrans.GetObject(acBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                if (acBlockTableRecord == null) return;

                foreach (var blkId in acBlockTableRecord)
                {
                    var acBlock = acTrans.GetObject(blkId, OpenMode.ForWrite) as BlockReference;
                    if (acBlock == null) continue;
                    
                    if (!acBlock.Name.Equals(BlockName, StringComparison.CurrentCultureIgnoreCase)) continue;
                    
                    foreach (ObjectId attId in acBlock.AttributeCollection)
                    {
                        var acAtt = acTrans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (acAtt == null) continue;

                        if (!Attributes.ContainsKey(acAtt.Tag)) continue;

                        acAtt.UpgradeOpen();
                        acAtt.TextString = Attributes[acAtt.Tag];
                    }
                }
                acTrans.Commit();
            }
        }

        [CommandMethod("REVALL")]
        public void RevAllDwgFiles()
        {
            Task t = Task.Run(() =>
            {
                // store the current database
                Database currentDatabase = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                try
                {                  
                    DirectoryInfo d = new DirectoryInfo(path);
                    FileInfo[] Files = d.GetFiles("*.dwg");

                    foreach (FileInfo file in Files)
                    {
                        var fileName = Path.GetFileName(file.FullName);
                        string dwgFlpath = path + fileName;

                        using (Database acDb = new Database(false, true))
                        {
                            acDb.ReadDwgFile(dwgFlpath, FileOpenMode.OpenForReadAndAllShare, false, null);
                            
                            HostApplicationServices.WorkingDatabase = acDb;

                            using (Transaction acTrans = acDb.TransactionManager.StartTransaction())
                            {
                                var acBlockTable = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                                if (acBlockTable == null) return;

                                var acBlockTableRecord = acTrans.GetObject(acBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                                if (acBlockTableRecord == null) return;

                                foreach (var blkId in acBlockTableRecord)
                                {
                                    var acBlock = acTrans.GetObject(blkId, OpenMode.ForWrite) as BlockReference;
                                    if (acBlock == null) continue;

                                    if (!acBlock.Name.Equals(BlockName, StringComparison.CurrentCultureIgnoreCase)) continue;

                                    foreach (ObjectId attId in acBlock.AttributeCollection)
                                    {
                                        var acAtt = acTrans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                                        if (acAtt == null) continue;

                                        if (!Attributes.ContainsKey(acAtt.Tag)) continue;

                                        acAtt.UpgradeOpen();
                                        acAtt.TextString = Attributes[acAtt.Tag];
                                    }
                                }
                                acTrans.Commit();
                            }
                            acDb.SaveAs(dwgFlpath, DwgVersion.AC1027);
                            // reset the current database as working database
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
        
    }
}
