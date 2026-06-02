using Microsoft.EntityFrameworkCore;
using UjangKasir.Desktop.Data;
using UjangKasir.Desktop.Models;

namespace UjangKasir.Desktop.Services;

public class MemberService(Func<AppDbContext> createDb)
{
    public async Task<List<Member>> SearchAsync(string keyword, bool activeOnly = false)
    {
        await using var db = createDb();
        var query = db.Members.AsNoTracking().AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim();
            query = query.Where(x =>
                x.MemberCode.Contains(search) ||
                x.Name.Contains(search) ||
                x.Phone.Contains(search));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        await using var db = createDb();
        return await db.Members.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task SaveAsync(Member member)
    {
        if (string.IsNullOrWhiteSpace(member.Name))
        {
            throw new InvalidOperationException("Nama member wajib diisi.");
        }

        await using var db = createDb();
        if (!string.IsNullOrWhiteSpace(member.MemberCode))
        {
            var exists = await db.Members.AnyAsync(x => x.MemberCode == member.MemberCode.Trim() && x.Id != member.Id);
            if (exists)
            {
                throw new InvalidOperationException("Kode member sudah digunakan.");
            }
        }

        if (member.Id == 0)
        {
            member.MemberCode = string.IsNullOrWhiteSpace(member.MemberCode)
                ? await GenerateMemberCodeAsync(db)
                : member.MemberCode.Trim();
            member.CreatedAt = DateTime.UtcNow;
            db.Members.Add(member);
        }
        else
        {
            var existing = await db.Members.FirstAsync(x => x.Id == member.Id);
            existing.MemberCode = member.MemberCode.Trim();
            existing.Name = member.Name.Trim();
            existing.Phone = member.Phone.Trim();
            existing.Email = member.Email.Trim();
            existing.Address = member.Address.Trim();
            existing.Points = member.Points;
            existing.IsActive = member.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        await using var db = createDb();
        var member = await db.Members.FirstAsync(x => x.Id == id);
        member.IsActive = false;
        member.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task AddPointsAsync(AppDbContext db, int memberId, decimal saleTotal)
    {
        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == memberId && x.IsActive);
        if (member is null)
        {
            return;
        }

        member.Points += Math.Max(0, (int)Math.Floor(saleTotal / 10000m));
        member.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task<string> GenerateMemberCodeAsync(AppDbContext db)
    {
        var prefix = $"MBR-{DateTime.Now:yyyyMMdd}-";
        var count = await db.Members.CountAsync();
        var next = count + 1;
        string code;
        do
        {
            code = $"{prefix}{next:0000}";
            next++;
        }
        while (await db.Members.AnyAsync(x => x.MemberCode == code));

        return code;
    }
}
