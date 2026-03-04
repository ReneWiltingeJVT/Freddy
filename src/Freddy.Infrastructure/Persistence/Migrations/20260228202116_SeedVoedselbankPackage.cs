using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freddy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedVoedselbankPackage : Migration
    {
        private const string PackageId = "019573a0-0000-7000-8000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO packages (id, name, description, content, keywords, is_active, created_at, updated_at)
                VALUES (
                    '{PackageId}',
                    'Voedselbank',
                    'Protocol voor het aanvragen en uitdelen van voedselbankpakketten aan cliënten in de thuiszorg.',
                    '## Voedselbank – Protocol

                **Wanneer:** Een cliënt heeft onvoldoende middelen voor dagelijkse boodschappen.

                ### Stappen
                1. **Signaleer** – Bespreek het onderwerp respectvol met de cliënt.
                2. **Check inkomen** – Cliënt moet onder 130% van het sociaal minimum zitten.
                3. **Aanvraagformulier** – Download via intranet > Formulieren > Voedselbank.
                4. **Bewijsstukken** – Kopie ID, inkomensverklaring (max 3 maanden oud), huurspecificatie.
                5. **Indienen** – Mail het formulier + bewijsstukken naar voedselbank@regiovoorbeeld.nl.
                6. **Doorlooptijd** – Gemiddeld 5-10 werkdagen.
                7. **Uitgifte** – Cliënt ontvangt wekelijks een pakket bij het dichtstbijzijnde uitgiftepunt.
                8. **Evaluatie** – Bespreek na 3 maanden of de situatie is veranderd.

                ### Veelgestelde vragen
                - *Mag ik namens de cliënt aanvragen?* Ja, met schriftelijke machtiging.
                - *Wat als de cliënt geen vast adres heeft?* Neem contact op met de maatschappelijk werker.
                - *Hoe lang loopt een toekenning?* Standaard 1 jaar, daarna herbeoordeling.',
                    ARRAY['voedselbank', 'voedselpakket', 'armoede', 'boodschappen', 'sociaal minimum', 'aanvraag'],
                    true,
                    NOW(),
                    NOW()
                )
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM packages WHERE id = '{PackageId}';");
        }
    }
}
