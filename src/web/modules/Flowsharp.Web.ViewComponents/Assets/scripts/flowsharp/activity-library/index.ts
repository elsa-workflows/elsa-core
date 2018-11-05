///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='./activity-library.ts' />

$(function(){
    const activityLibraryContainer: HTMLDivElement = <HTMLDivElement>document.getElementById('flowsharp-activity-library');
    const activityLibrary = new ActivityLibrary(activityLibraryContainer);
});
