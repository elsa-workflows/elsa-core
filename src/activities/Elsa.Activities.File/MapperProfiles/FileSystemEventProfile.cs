using AutoMapper;
using Elsa.Activities.File.Models;
using System;
using System.IO;

namespace Elsa.Activities.File.MapperProfiles
{
    public class FileSystemEventProfile : Profile
    {
        public FileSystemEventProfile()
        {
            CreateMap<FileSystemEventArgs, FileSystemEvent>()
                .ForMember(d => d.ChangeType,
                o => o.MapFrom(s => s.ChangeType))
                .ForMember(d => d.Directory,
                 o => o.MapFrom(s => Path.GetDirectoryName(s.FullPath)))
                .ForMember(d => d.FileName,
                o => o.MapFrom(s => s.Name))
                .ForMember(d => d.FullPath,
                o => o.MapFrom(s => s.FullPath))
                .ForMember(d => d.TimeStamp,
                o => o.MapFrom(s => DateTime.Now));

            CreateMap<RenamedEventArgs, FileSystemEvent>()
                .IncludeBase<FileSystemEventArgs, FileSystemEvent>()
                .ForMember(d => d.OldFileName,
                o => o.MapFrom(s => s.OldName))
                .ForMember(d => d.OldFullPath,
                 o => o.MapFrom(s => Path.GetDirectoryName(s.OldFullPath)));
        }
    }
}
