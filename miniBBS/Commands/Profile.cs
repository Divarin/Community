using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using miniBBS.Services;
using System;
using System.Collections.Generic;

namespace miniBBS.Commands
{
    public static class Profile
    {
        public static void Execute(BbsSession session)
        {
            var di = GlobalDependencyResolver.Default;

            var metaRepo = di.GetRepository<Metadata>();
            var profile = metaRepo.Get(new Dictionary<string, object>
            {
                { nameof(Metadata.UserId), session.User.Id },
                { nameof(Metadata.Type), MetadataType.Profile },
            }).PruneAllButMostRecent(di);

            var profileText = profile?.Data ?? string.Empty;

            var editor = di.Get<ITextEditor>();

            editor.OnSave = body =>
            {
                if (body?.Length > Constants.MaxProfileLength)
                {
                    session.Io.Error("Warning, profile exceeds maximum display length (one screen at 80x25).");
                }

                if (string.IsNullOrWhiteSpace(body) && profile != null)
                {
                    metaRepo.Delete(profile);
                    return "Profile is empty, deleting profile";
                }
                else if (string.IsNullOrWhiteSpace(body))
                {
                    return "Profile is empty, profile not saved";
                }
                else if (profile == null)
                {
                    profile = new Metadata
                    {
                        Type = MetadataType.Profile,
                        UserId = session.User.Id,
                        Data = body,
                        DateAddedUtc = DateTime.UtcNow,
                    };
                    metaRepo.Insert(profile);
                    return $"New profile saved, view with /ui {session.User.Name}  Save an empty profile to delete it";
                }
                else
                {
                    profile.Data = body;
                    profile.DateAddedUtc = DateTime.UtcNow;
                    metaRepo.Update(profile);
                    return $"Profile updated, view with /ui {session.User.Name}  Save an empty profile to delete it";
                }
            };

            editor.EditText(session, new LineEditorParameters
            {
                PreloadedBody = profileText,
                QuitOnSave = true,
            });
        }
    }
}
