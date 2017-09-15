Assets are added to the projects via MSBuild wildcards.
So if you want to add new assets - simply copy them to the root directory UrhoAssets.
Don't do it via VS.

    <X Include="..\UrhoAssets\**\*.*">
      <Link>UrhoAssets\%(RecursiveDir)\%(Filename)%(Extension)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </X>
