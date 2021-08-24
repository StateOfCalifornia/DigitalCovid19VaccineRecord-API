using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;

namespace Application.Common.Mappings
{
    public interface IMapFrom<T>
    {
        void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
    }
}
