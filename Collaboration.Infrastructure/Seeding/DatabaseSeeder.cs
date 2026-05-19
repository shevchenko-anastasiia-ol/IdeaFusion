using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Seeding;

public class DatabaseSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
 
    // Real Identity users (Guid IDs from IdentitySeeder)
    private static readonly UserSnapshot AnnaUser  = new("22222222-2222-2222-2222-222222222222", "AnnoGray",   null);
    private static readonly UserSnapshot VladUser  = new("44444444-4444-4444-4444-444444444444", "VladBibber", null);
    private static readonly UserSnapshot EmilyUser = new("66666666-6666-6666-6666-666666666666", "EmilyFox",   null);

    // Системний snapshot для seed-даних
    private static readonly UserSnapshot SystemUser = new("system", "System", null);
 
    // Тестові юзери з UserService (денормалізовані snapshots)
    private static readonly UserSnapshot Artist01    = new("user_artist_01",       "pixel_master",    "https://cdn.example.com/avatars/artist01.jpg");
    private static readonly UserSnapshot Sound01     = new("user_sound_01",        "soundwave_pro",   null);
    private static readonly UserSnapshot Drummer01   = new("user_drummer_01",      "beat_keeper",     "https://cdn.example.com/avatars/drummer01.jpg");
    private static readonly UserSnapshot Editor01    = new("user_editor_01",       "word_smith",      null);
    private static readonly UserSnapshot Videographer01 = new("user_videographer_01", "lens_eye",    "https://cdn.example.com/avatars/video01.jpg");
    private static readonly UserSnapshot Motion01    = new("user_motion_01",       "motion_lab",      "https://cdn.example.com/avatars/motion01.jpg");
    private static readonly UserSnapshot Keyboardist01 = new("user_keyboardist_01", "keys_master",   null);
 
    public DatabaseSeeder(MongoDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }
 
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting database seeding ===");
 
        try
        {
            await SeedTeamsAsync(cancellationToken);
            await SeedCollaborationRequestsAsync(cancellationToken);
            await SeedGroupInvitationsAsync(cancellationToken);
            await SeedTeamPostsAsync(cancellationToken);
            await SeedRealUserTeamsAsync(cancellationToken);
            await SeedRealUserInvitationsAsync(cancellationToken);
            await SeedRealUserCollaborationRequestsAsync(cancellationToken);

            _logger.LogInformation("=== Database seeding completed successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! CRITICAL ERROR during database seeding !!!");
            throw;
        }
    }
 
    private async Task SeedTeamsAsync(CancellationToken cancellationToken)
    {
        var teams = new List<Team>
        {
            new Team(
                name: "Indie Game Dev Crew",
                description: "Шукаємо однодумців для розробки невеликої інді-гри у стилі pixel art. Є готовий концепт та частина механік.",
                category: "GameDev",
                tags: ["gamedev", "pixelart", "unity", "indie"],
                owner: SystemUser
            ),
            new Team(
                name: "Soundwave Collective",
                description: "Музичний гурт у пошуку барабанщика та клавішника для запису дебютного EP у стилі indie-rock.",
                category: "Music",
                tags: ["music", "indierock", "band", "recording"],
                owner: SystemUser
            ),
            new Team(
                name: "Visual Stories",
                description: "Команда фотографів та відеографів для спільних комерційних та арт-проєктів.",
                category: "Photography",
                tags: ["photo", "video", "art", "commercial"],
                owner: SystemUser
            ),
            new Team(
                name: "Open Source Writers",
                description: "Колектив письменників для написання спільної антології оповідань у жанрі наукової фантастики.",
                category: "Writing",
                tags: ["writing", "scifi", "anthology", "fiction"],
                owner: SystemUser
            ),
            new Team(
                name: "Motion & Design Lab",
                description: "Дизайнери та моушн-артисти для створення анімованого контенту та брендингу.",
                category: "Design",
                tags: ["design", "motion", "branding", "animation"],
                owner: SystemUser
            )
        };
 
        teams[0].AddRequiredRole("2D Artist", "Досвід роботи з pixel art, знання Aseprite", "system");
        teams[0].AddRequiredRole("Sound Designer", "Створення ігрових звуків та музики", "system");
 
        teams[1].AddRequiredRole("Drummer", "Досвід від 2 років, можливість репетицій у Києві", "system");
        teams[1].AddRequiredRole("Keyboardist", "Знання музичної теорії, досвід запису у студії", "system");
 
        teams[2].AddRequiredRole("Videographer", "Досвід з камерами Sony/Canon, знання DaVinci Resolve", "system");
        teams[2].AddMember(Videographer01, "Videographer");
        teams[2].SetStatus(TeamStatus.Active, "system");
 
        teams[3].AddRequiredRole("Editor", "Досвід редагування художніх текстів", "system");
        teams[3].AddRequiredRole("Illustrator", "Ілюстрації для обкладинки та розділів", "system");
 
        teams[4].AddRequiredRole("Motion Designer", "After Effects, Cinema 4D", "system");
        teams[4].AddMember(Motion01, "Motion Designer");
        teams[4].SetStatus(TeamStatus.Active, "system");
 
        foreach (var team in teams)
        {
            var exists = await _context.Teams
                .Find(t => t.Name == team.Name && !t.IsDeleted)
                .AnyAsync(cancellationToken);
 
            if (!exists)
            {
                await _context.Teams.InsertOneAsync(team, cancellationToken: cancellationToken);
                _logger.LogInformation("Inserted team: {Name}", team.Name);
            }
        }
    }
 
    private async Task SeedCollaborationRequestsAsync(CancellationToken cancellationToken)
    {
        var gameDevTeam = await _context.Teams
            .Find(t => t.Name == "Indie Game Dev Crew" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        var musicTeam = await _context.Teams
            .Find(t => t.Name == "Soundwave Collective" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        var writingTeam = await _context.Teams
            .Find(t => t.Name == "Open Source Writers" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        if (gameDevTeam == null || musicTeam == null || writingTeam == null)
        {
            _logger.LogWarning("Teams not found. Skipping collaboration requests seeding.");
            return;
        }
 
        var requests = new List<CollaborationRequest>
        {
            new CollaborationRequest(
                teamId: gameDevTeam.Id,
                fromUserId: Artist01.UserId,
                role: "2D Artist",
                message: "Маю 3 роки досвіду з pixel art, знаю Aseprite та Photoshop. Долучу портфоліо за запитом.",
                toUserId: null
            ),
            new CollaborationRequest(
                teamId: gameDevTeam.Id,
                fromUserId: Sound01.UserId,
                role: "Sound Designer",
                message: "Працював над кількома інді-проєктами, є свій домашній саунд-дизайн стек.",
                toUserId: null
            ),
            new CollaborationRequest(
                teamId: musicTeam.Id,
                fromUserId: Drummer01.UserId,
                role: "Drummer",
                message: "5 років досвіду, живу у Києві, є власний інструмент.",
                toUserId: null
            ),
            new CollaborationRequest(
                teamId: writingTeam.Id,
                fromUserId: Editor01.UserId,
                role: "Editor",
                message: "Редагував збірки оповідань для кількох незалежних видавництв.",
                toUserId: null
            ),
            new CollaborationRequest(
                teamId: writingTeam.Id,
                fromUserId: "user_writer_02",
                role: "Editor",
                message: "Маю досвід у літературному редагуванні та коректурі.",
                toUserId: null
            )
        };
 
        requests[1].Accept("system");
        requests[4].Reject("system");
 
        foreach (var request in requests)
        {
            var exists = await _context.CollaborationRequests
                .Find(r => r.TeamId == request.TeamId && r.FromUserId == request.FromUserId && r.Role == request.Role)
                .AnyAsync(cancellationToken);
 
            if (!exists)
            {
                await _context.CollaborationRequests.InsertOneAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation("Inserted collaboration request for team {TeamId} from user {UserId}",
                    request.TeamId, request.FromUserId);
            }
        }
    }
 
    private async Task SeedGroupInvitationsAsync(CancellationToken cancellationToken)
    {
        var photoTeam = await _context.Teams
            .Find(t => t.Name == "Visual Stories" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        var designTeam = await _context.Teams
            .Find(t => t.Name == "Motion & Design Lab" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        var musicTeam = await _context.Teams
            .Find(t => t.Name == "Soundwave Collective" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        if (photoTeam == null || designTeam == null || musicTeam == null)
        {
            _logger.LogWarning("Teams not found. Skipping group invitations seeding.");
            return;
        }
 
        var invitations = new List<GroupInvitation>
        {
            new GroupInvitation(
                teamId: photoTeam.Id,
                invitedUserId: Videographer01.UserId,
                invitedByUserId: "system",
                role: "Videographer",
                message: "Бачили ваше портфоліо — ваш стиль ідеально підходить до нашої команди!",
                expirationDays: 7
            ),
            new GroupInvitation(
                teamId: photoTeam.Id,
                invitedUserId: "user_photographer_02",
                invitedByUserId: "system",
                role: "Photographer",
                message: "Запрошуємо до спільного проєкту для міського бренду.",
                expirationDays: 14
            ),
            new GroupInvitation(
                teamId: designTeam.Id,
                invitedUserId: Motion01.UserId,
                invitedByUserId: "system",
                role: "Motion Designer",
                message: "Маємо цікавий проєкт, де ваші навички з After Effects будуть дуже доречні.",
                expirationDays: 10
            ),
            new GroupInvitation(
                teamId: musicTeam.Id,
                invitedUserId: Keyboardist01.UserId,
                invitedByUserId: "system",
                role: "Keyboardist",
                message: "Слухали ваші записи — хочемо запросити вас до нашого гурту!",
                expirationDays: 7
            ),
            new GroupInvitation(
                teamId: designTeam.Id,
                invitedUserId: "user_illustrator_01",
                invitedByUserId: "system",
                role: "Illustrator",
                message: "Потрібен ілюстратор для нового брендингового проєкту.",
                expirationDays: 5
            )
        };
 
        invitations[1].Accept("user_photographer_02");
        invitations[4].Decline("user_illustrator_01");
 
        foreach (var invitation in invitations)
        {
            var exists = await _context.GroupInvitations
                .Find(i => i.TeamId == invitation.TeamId && i.InvitedUserId == invitation.InvitedUserId && i.Role == invitation.Role)
                .AnyAsync(cancellationToken);
 
            if (!exists)
            {
                await _context.GroupInvitations.InsertOneAsync(invitation, cancellationToken: cancellationToken);
                _logger.LogInformation("Inserted group invitation for team {TeamId} to user {UserId}",
                    invitation.TeamId, invitation.InvitedUserId);
            }
        }
    }
 
    private async Task SeedTeamPostsAsync(CancellationToken cancellationToken)
    {
        var visualTeam = await _context.Teams
            .Find(t => t.Name == "Visual Stories" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        var designTeam = await _context.Teams
            .Find(t => t.Name == "Motion & Design Lab" && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
 
        if (visualTeam == null || designTeam == null)
        {
            _logger.LogWarning("Teams not found. Skipping team posts seeding.");
            return;
        }
 
        var posts = new List<TeamPost>
        {
            new TeamPost(
                postId: "post_content_001",
                teamId: visualTeam.Id,
                author: Videographer01,
                title: "Наш перший спільний фотопроєкт — міські пейзажі Києва"
            ),
            new TeamPost(
                postId: "post_content_002",
                teamId: visualTeam.Id,
                author: Videographer01,
                title: "Behind the scenes: зйомка для бренду Kyiv Coffee"
            ),
            new TeamPost(
                postId: "post_content_003",
                teamId: designTeam.Id,
                author: Motion01,
                title: "Анімований логотип для стартапу — case study"
            ),
        };
 
        foreach (var post in posts)
        {
            var exists = await _context.TeamPosts
                .Find(p => p.TeamId == post.TeamId && p.PostId == post.PostId)
                .AnyAsync(cancellationToken);

            if (!exists)
            {
                await _context.TeamPosts.InsertOneAsync(post, cancellationToken: cancellationToken);
                _logger.LogInformation("Inserted team post {PostId} for team {TeamId}", post.PostId, post.TeamId);
            }
        }
    }

    // ── Real-user teams ──────────────────────────────────────────────────────

    private async Task SeedRealUserTeamsAsync(CancellationToken cancellationToken)
    {
        var realTeams = new List<Team>
        {
            new Team(
                name: "UX Collective",
                description: "Команда UI/UX дизайнерів для спільних проєктів — від мобільних застосунків до веб-продуктів.",
                category: "Design",
                tags: ["design", "ui-ux", "figma", "branding"],
                owner: AnnaUser
            ),
            new Team(
                name: "Digital Sound Lab",
                description: "Студія електронної та цифрової музики для запису, продюсування та колаборацій.",
                category: "Music",
                tags: ["music", "digital", "production", "sound"],
                owner: VladUser
            ),
            new Team(
                name: "PixelCraft Studio",
                description: "3D-арт та анімація для ігор, кіно та цифрового мистецтва.",
                category: "Animation",
                tags: ["3d", "animation", "art", "gamedev"],
                owner: EmilyUser
            ),
        };

        realTeams[0].AddRequiredRole("Motion Designer", "After Effects або Lottie", "system");
        realTeams[1].AddRequiredRole("Mixing Engineer", "Зведення треків, досвід з Ableton", "system");
        realTeams[2].AddRequiredRole("Concept Artist", "2D-концепти для ігрового світу", "system");
        realTeams[2].AddRequiredRole("Rigger",         "Rigging персонажів у Blender", "system");
        realTeams[2].SetStatus(TeamStatus.Active, "system");

        foreach (var team in realTeams)
        {
            var exists = await _context.Teams
                .Find(t => t.Name == team.Name && !t.IsDeleted)
                .AnyAsync(cancellationToken);

            if (!exists)
            {
                await _context.Teams.InsertOneAsync(team, cancellationToken: cancellationToken);
                _logger.LogInformation("RealDataSeeder: inserted team '{Name}'", team.Name);
            }
        }
    }

    // ── Real-user collaboration requests ────────────────────────────────────

    private async Task SeedRealUserCollaborationRequestsAsync(CancellationToken cancellationToken)
    {
        // Anna → Soundwave Collective (owned by system, not by Anna)
        // Anna → Motion & Design Lab
        // Vlad → Indie Game Dev Crew
        // Vlad → PixelCraft Studio (owned by Emily)
        // Emily → UX Collective (owned by Anna)
        // Emily → Open Source Writers
        var targets = new[]
        {
            ("Soundwave Collective",  AnnaUser,  "Keyboardist",     "Граю на клавішних 6 років, є студійний досвід запису.",          (string?)null),
            ("Motion & Design Lab",   AnnaUser,  "Motion Designer", "Знаю After Effects та Lottie, є портфоліо анімованих брендингів.", null),
            ("Indie Game Dev Crew",   VladUser,  "Sound Designer",  "Займаюсь звукодизайном для ігор, маю власний DAW-стек.",           null),
            ("PixelCraft Studio",     VladUser,  "Concept Artist",  "Малюю концепти персонажів для ігор уже 4 роки.",                  EmilyUser.UserId),
            ("UX Collective",         EmilyUser, "Motion Designer", "Спеціалізуюсь на UI-анімаціях у Figma та Lottie.",                 AnnaUser.UserId),
            ("Open Source Writers",   EmilyUser, "Illustrator",     "Ілюстратор з досвідом оформлення книг та коміксів.",              null),
        };

        foreach (var (teamName, fromUser, role, message, toUserId) in targets)
        {
            var team = await _context.Teams
                .Find(t => t.Name == teamName && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("RealDataSeeder: team '{Name}' not found — skipping collaboration request", teamName);
                continue;
            }

            var alreadyExists = await _context.CollaborationRequests
                .Find(r => r.TeamId == team.Id && r.FromUserId == fromUser.UserId && r.Role == role)
                .AnyAsync(cancellationToken);

            if (!alreadyExists)
            {
                var req = new CollaborationRequest(
                    teamId: team.Id,
                    fromUserId: fromUser.UserId,
                    role: role,
                    message: message,
                    toUserId: toUserId,
                    fromUsername: fromUser.Username,
                    fromAvatarUrl: fromUser.AvatarUrl);

                await _context.CollaborationRequests.InsertOneAsync(req, cancellationToken: cancellationToken);
                _logger.LogInformation("RealDataSeeder: collaboration request from {UserId} to '{Team}' as {Role}",
                    fromUser.UserId, teamName, role);
            }
        }
    }

    // ── Real-user invitations ────────────────────────────────────────────────

    private async Task SeedRealUserInvitationsAsync(CancellationToken cancellationToken)
    {
        // Invite real users to existing system-seeded teams
        var targets = new[]
        {
            ("Indie Game Dev Crew",    AnnaUser.UserId,  "UI/UX Designer",   "Вашу роботу бачили — дуже потрібен UI-дизайнер для нашого проєкту!", 14),
            ("Soundwave Collective",   VladUser.UserId,  "Keyboardist",      "Слухали ваші треки — будемо раді мати вас у команді!",               10),
            ("Motion & Design Lab",    EmilyUser.UserId, "3D Artist",        "Ваша анімація ідеально підходить до стилю нашого проєкту.",           7),
            ("Visual Stories",         AnnaUser.UserId,  "Brand Designer",   "Шукаємо дизайнера для оформлення нашого нового проєкту.",            10),
            ("Indie Game Dev Crew",    EmilyUser.UserId, "3D Artist",        "3D-артист дуже потрібен — у нас вже готовий концепт персонажа.",       14),
        };

        foreach (var (teamName, invitedId, role, message, days) in targets)
        {
            var team = await _context.Teams
                .Find(t => t.Name == teamName && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("RealDataSeeder: team '{Name}' not found — skipping invitation", teamName);
                continue;
            }

            var alreadyExists = await _context.GroupInvitations
                .Find(i => i.TeamId == team.Id && i.InvitedUserId == invitedId && i.Role == role)
                .AnyAsync(cancellationToken);

            if (!alreadyExists)
            {
                var inv = new GroupInvitation(
                    teamId: team.Id,
                    invitedUserId: invitedId,
                    invitedByUserId: "system",
                    role: role,
                    message: message,
                    expirationDays: days);

                await _context.GroupInvitations.InsertOneAsync(inv, cancellationToken: cancellationToken);
                _logger.LogInformation("RealDataSeeder: invited user {UserId} to '{Team}' as {Role}", invitedId, teamName, role);
            }
        }
    }
}