### MsBuild Integration ###

```xml
<UsingTask TaskName="Git" AssemblyFile="path/to/GitTask.dll" />

...

<Git DependencyFile="$(MSBuildProjectDirectory)\msbuild\git.json">
      <Output TaskParameter="Names" ItemName="GitProjects" />
</Git>
```

### JSON Dependency Attributes ###

Name : Dependency Name

####Git Clone / Pull options :
TopFolder : Directory where to clone sources
Remote : Remote Url (can be http/git/file protocols)
Branch : Branch Name
Commit : Commit Reference

####Direct Compilation :
LocalFolder : Folder where to find sources for this dependency
### JSON example ###

