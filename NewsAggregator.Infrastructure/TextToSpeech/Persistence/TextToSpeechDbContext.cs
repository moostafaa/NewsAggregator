using Microsoft.EntityFrameworkCore;
using NewsAggregator.Domain.TextToSpeech.Entities;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;
using System.Text.Json;

namespace NewsAggregator.Infrastructure.TextToSpeech.Persistence
{
    public class TextToSpeechDbContext : DbContext
    {
        public DbSet<AudioConversion> AudioConversions { get; set; }
        public DbSet<VoiceConfigurationData> VoiceConfigurations { get; set; }
        public DbSet<AudioEntity> AudioFiles { get; set; }

        public TextToSpeechDbContext(DbContextOptions<TextToSpeechDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AudioConversion>(entity =>
            {
                entity.ToTable("AudioConversions");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(e => e.VoiceConfig)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<VoiceConfiguration>(v, new JsonSerializerOptions()));

                entity.Property(e => e.AudioFormat)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.StoragePath)
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .IsRequired();

                entity.Property(e => e.ErrorMessage)
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<VoiceConfigurationData>(entity =>
            {
                entity.ToTable("VoiceConfigurations");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.VoiceId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LanguageCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Gender)
                    .HasMaxLength(20);

                entity.Property(e => e.Provider)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.IsDefault)
                    .IsRequired();

                entity.HasIndex(e => new { e.LanguageCode, e.Provider, e.IsDefault });
            });
            
            modelBuilder.Entity<AudioEntity>(entity =>
            {
                entity.ToTable("AudioFiles");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(255);
                    
                entity.Property(e => e.MimeType)
                    .IsRequired()
                    .HasMaxLength(100);
                    
                entity.Property(e => e.AudioData)
                    .IsRequired();
                    
                entity.Property(e => e.Metadata)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.FileSize)
                    .IsRequired();
                    
                entity.Property(e => e.OriginalText)
                    .HasMaxLength(4000);
                    
                // Create indexes for common query patterns
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ArticleId);
            });
        }
    }

    public class VoiceConfigurationData
    {
        public Guid Id { get; set; }
        public string VoiceId { get; set; }
        public string LanguageCode { get; set; }
        public string Gender { get; set; }
        public string Provider { get; set; }
        public float SpeakingRate { get; set; }
        public float Pitch { get; set; }
        public bool IsDefault { get; set; }

        public VoiceConfiguration ToDomainModel()
        {
            return VoiceConfiguration.Create(
                VoiceId,
                LanguageCode,
                Gender,
                Provider,
                SpeakingRate,
                Pitch);
        }

        public static VoiceConfigurationData FromDomainModel(VoiceConfiguration config, bool isDefault = false)
        {
            return new VoiceConfigurationData
            {
                Id = Guid.NewGuid(),
                VoiceId = config.VoiceId,
                LanguageCode = config.LanguageCode,
                Gender = config.Gender,
                Provider = config.Provider,
                SpeakingRate = config.SpeakingRate,
                Pitch = config.Pitch,
                IsDefault = isDefault
            };
        }
    }
} 