using Microsoft.AnalysisServices.Tabular;


string sourceFolder = @"C:\pbi\DIAD\";  //path to a template PBIP project, pointing to the folder containing the .pbip file
string targetFolder = @"C:\pbi\DIAD_new\"; //target folder for new PBIP project. 

if (Directory.Exists(targetFolder))
    Directory.Delete(targetFolder, true);

var newDb = "Client123DB";
var newServer = "Client123DB";

CopyProjectAndTransformModel(sourceFolder, targetFolder, m =>
{
    foreach (var t in m.Tables)
    {
        foreach (var p in t.Partitions.Where(p => p.Source.GetType() == typeof(MPartitionSource)))
        {
            MPartitionSource source = (MPartitionSource)p.Source;
            var query = source.Expression;

            query = query.Replace("<database>", newDb);
            query = query.Replace("<server>", newServer);
            source.Expression = query;
        }
    }
});
return;

void CopyProjectAndTransformModel(string sourceProject, string targetProject, Action<Model> modelTransform)
{
    var tdi = new DirectoryInfo(targetProject);
    if (tdi.Exists)
        throw new InvalidOperationException($"Target Folder Already Exists: {targetProject}.");

    CopyDirectory(sourceProject, targetProject, true);

    var db = LoadDatabase(targetProject);

    //this will eventually not be necessary, and is only needed for models with no tables
    if (db.Model.DefaultPowerBIDataSourceVersion == PowerBIDataSourceVersion.PowerBI_V1 ||
            db.Model.DefaultPowerBIDataSourceVersion == PowerBIDataSourceVersion.PowerBI_V2)
    {
        db.Model.DefaultPowerBIDataSourceVersion = PowerBIDataSourceVersion.PowerBI_V3;
    }

    modelTransform(db.Model);

    UpdateSemanticModel(targetProject, db);


}


static string GetSemanticModelFolder(string projectFolder)
{
    var tdi = new DirectoryInfo(projectFolder);

    var tmdlDi = tdi.EnumerateDirectories()
                .Where(di => di.Name.EndsWith("SemanticModel"))
                .First()
                .GetDirectories()
                .Where(d => d.Name == "definition")
                .Single();

    return tmdlDi.FullName;
}
static void UpdateSemanticModel(string projectFolder, Database db)
{
    var tmdlFolder = GetSemanticModelFolder(projectFolder);
    TmdlSerializer.SerializeDatabaseToFolder(db, tmdlFolder);
}

static Database LoadDatabase(string projectFolder)
{

    string tmdlFolder = GetSemanticModelFolder(projectFolder);
    var db = TmdlSerializer.DeserializeDatabaseFromFolder(tmdlFolder);
    return db;
}

static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
{
    // Get information about the source directory
    var dir = new DirectoryInfo(sourceDir);

    // Check if the source directory exists
    if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

    // Cache directories before we start copying
    DirectoryInfo[] dirs = dir.GetDirectories();

    // Create the destination directory
    Directory.CreateDirectory(destinationDir);

    // Get the files in the source directory and copy to the destination directory
    foreach (FileInfo file in dir.GetFiles())
    {
        string targetFilePath = Path.Combine(destinationDir, file.Name);
        file.CopyTo(targetFilePath);
    }

    // If recursive and copying subdirectories, recursively call this method
    if (recursive)
    {
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }
}
