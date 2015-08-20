### MsBuild Integration ###

```xml
<UsingTask TaskName="Git" AssemblyFile="path/to/GitTask.dll" />

...

<Git DependencyFile="$(MSBuildProjectDirectory)\msbuild\git.json">
      <Output TaskParameter="Names" ItemName="GitProjects" />
</Git>
```