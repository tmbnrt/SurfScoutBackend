using SurfScoutBackend.Models.DTOs;
using SurfScoutBackend.Models.WindFieldModel;
using SurfScoutBackend.Models;
using Microsoft.AspNetCore.Http;

namespace SurfScoutBackend.Functions
{
    public static class DtoMapper
    {
        public static List<SessionDto> SessionToDtoList(IEnumerable<Session> sessions)
        {
            return sessions.Select(SessionToDto).ToList();
        }

        public static SessionDto SessionToDto(Session session)
        {
            return new SessionDto
            {
                Id = session.Id,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                SpotId = session.Spotid,
                UserId = session.UserId,
                Sport = session.Sport,
                Sail_size = session.Sail_size,
                Rating = session.Rating,
                Wave_height = session.Wave_height,
                WindSpeedKnots = session.WindSpeedKnots,
                WindDirectionDegree = session.WindDirectionDegree
            };
        }



    }
}
