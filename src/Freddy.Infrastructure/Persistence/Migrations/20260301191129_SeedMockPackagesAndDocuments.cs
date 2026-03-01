using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freddy.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Seeds two additional mock packages (Medicatie in Beheer, Valpreventie) and adds documents to all three packages.
    /// </summary>
    public partial class SeedMockPackagesAndDocuments : Migration
    {
        private const string VoedselbankPackageId = "019573a0-0000-7000-8000-000000000001";
        private const string MedicatiePackageId = "019573a0-0000-7000-8000-000000000002";
        private const string ValpreventiePackageId = "019573a0-0000-7000-8000-000000000003";

        private const string VoedselbankDoc1Id = "019573a0-0000-7000-8000-000000000011";
        private const string VoedselbankDoc2Id = "019573a0-0000-7000-8000-000000000012";
        private const string MedicatieDoc1Id = "019573a0-0000-7000-8000-000000000021";
        private const string MedicatieDoc2Id = "019573a0-0000-7000-8000-000000000022";
        private const string ValpreventieDoc1Id = "019573a0-0000-7000-8000-000000000031";
        private const string ValpreventieDoc2Id = "019573a0-0000-7000-8000-000000000032";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Voedselbank: add synonyms and tags
            migrationBuilder.Sql($"""
                UPDATE packages
                SET synonyms = ARRAY['voedselhulp', 'voedsel', 'eten', 'boodschappen hulp'],
                    tags = ARRAY['voedselbank', 'voedselpakket', 'armoede', 'boodschappen', 'sociaal minimum', 'aanvraag', 'voedselhulp']
                WHERE id = '{VoedselbankPackageId}';
                """);

            // Insert Medicatie in Beheer
            migrationBuilder.Sql($"""
                INSERT INTO packages (id, title, description, content, tags, synonyms, is_published, requires_confirmation, created_at, updated_at)
                VALUES (
                    '{MedicatiePackageId}',
                    'Medicatie in Beheer',
                    'Protocol voor het beheren, uitdelen en registreren van medicatie voor cliënten.',
                    '## Medicatie in Beheer – Protocol

                **Wanneer:** Een cliënt heeft ondersteuning nodig bij het beheren of innemen van medicatie.

                ### Stappen
                1. **Medicatieoverzicht opvragen** – Vraag het actuele medicatieoverzicht op bij de apotheek.
                2. **Medicatielijst controleren** – Vergelijk het overzicht met wat de cliënt in huis heeft.
                3. **Baxterrol / weekdoos** – Controleer of de cliënt een baxterrol of weekdoos nodig heeft.
                4. **Toedienlijst invullen** – Registreer elke toediening op de toedienlijst (datum, tijd, paraaf).
                5. **Bijwerkingen signaleren** – Let op bijwerkingen en meld deze bij de huisarts.
                6. **Dubbele controle** – Bij risicogeneesmiddelen altijd een collega laten meekijken.
                7. **Bewaren** – Bewaar medicatie op de juiste temperatuur en buiten bereik van kinderen.
                8. **Overdracht** – Zorg dat medicatiewijzigingen doorgegeven worden aan het team.

                ### Veelgestelde vragen
                - *Wie mag medicatie toedienen?* Alleen bevoegde en bekwame zorgmedewerkers.
                - *Wat als een cliënt medicatie weigert?* Noteer dit en informeer de arts.
                - *Hoe ga ik om met opiaten?* Volg het opiatenprotocol (aparte sleutelkast, dubbele registratie).',
                    ARRAY['medicatie', 'medicijnen', 'pillen', 'uitgifte', 'apotheek', 'toedienlijst'],
                    ARRAY['medicijnen', 'pillen', 'geneesmiddelen', 'tabletten', 'baxterrol'],
                    true,
                    false,
                    NOW(),
                    NOW()
                )
                ON CONFLICT (id) DO NOTHING;
                """);

            // Insert Valpreventie
            migrationBuilder.Sql($"""
                INSERT INTO packages (id, title, description, content, tags, synonyms, is_published, requires_confirmation, created_at, updated_at)
                VALUES (
                    '{ValpreventiePackageId}',
                    'Valpreventie',
                    'Protocol voor het voorkomen van valincidenten bij ouderen en kwetsbare cliënten.',
                    '## Valpreventie – Protocol

                **Wanneer:** Een cliënt heeft een verhoogd valrisico of heeft recent een valincident gehad.

                ### Stappen
                1. **Valrisico inschatting** – Gebruik de Valrisico Checklist bij intake en na elk valincident.
                2. **Omgevingscheck** – Controleer de woning op valgevaar: losse kleedjes, drempels, verlichting, gladde vloeren.
                3. **Hulpmiddelen** – Adviseer over loophulpmiddelen (rollator, wandelstok) en laat deze aanmeten.
                4. **Schoeisel** – Controleer of de cliënt stevige, goed passende schoenen draagt.
                5. **Medicatiereview** – Bespreek met de arts of medicatie (bijv. bloeddrukverlagende middelen) bijdraagt aan valrisico.
                6. **Oefenprogramma** – Verwijs naar een fysiotherapeut voor balans- en krachttraining.
                7. **Noodknop** – Zorg dat de cliënt een persoonlijk alarmsysteem heeft.
                8. **Registratie** – Registreer elk valincident in het zorgsysteem (datum, situatie, letsel).

                ### Veelgestelde vragen
                - *Wanneer moet ik de huisarts informeren?* Bij elk valincident, ook als er geen letsel is.
                - *Mag ik zelf hulpmiddelen adviseren?* Ja, maar verwijs naar een ergotherapeut voor complexe situaties.
                - *Hoe vaak moet de omgevingscheck herhaald worden?* Minimaal elk halfjaar of na een wijziging in de woonsituatie.',
                    ARRAY['vallen', 'valpreventie', 'mobiliteit', 'ouderen', 'valincident', 'valrisico'],
                    ARRAY['vallen', 'uitglijden', 'struikelen', 'balans', 'rollator', 'loophulpmiddel'],
                    true,
                    false,
                    NOW(),
                    NOW()
                )
                ON CONFLICT (id) DO NOTHING;
                """);

            // Insert documents for Voedselbank
            migrationBuilder.Sql($"""
                INSERT INTO documents (id, package_id, name, description, type, file_url, created_at, updated_at)
                VALUES
                (
                    '{VoedselbankDoc1Id}',
                    '{VoedselbankPackageId}',
                    'Informatiepakket Voedselbank',
                    'Informatiebrochure over de voedselbank voor cliënten en medewerkers.',
                    'Pdf',
                    '/seed-documents/voedselbank-informatiepakket.pdf',
                    NOW(), NOW()
                ),
                (
                    '{VoedselbankDoc2Id}',
                    '{VoedselbankPackageId}',
                    'Aanvraagformulier Voedselbank',
                    'Excel-formulier voor het aanvragen van een voedselbankpakket.',
                    'Link',
                    '/seed-documents/voedselbank-aanvraagformulier.xlsx',
                    NOW(), NOW()
                )
                ON CONFLICT (id) DO NOTHING;
                """);

            // Insert documents for Medicatie in Beheer
            migrationBuilder.Sql($"""
                INSERT INTO documents (id, package_id, name, description, type, file_url, created_at, updated_at)
                VALUES
                (
                    '{MedicatieDoc1Id}',
                    '{MedicatiePackageId}',
                    'Medicatieprotocol Handboek',
                    'Volledig protocol voor medicatiebeheer en toediening.',
                    'Pdf',
                    '/seed-documents/medicatieprotocol-handboek.pdf',
                    NOW(), NOW()
                ),
                (
                    '{MedicatieDoc2Id}',
                    '{MedicatiePackageId}',
                    'Medicatielijst Template',
                    'Excel-template voor het bijhouden van medicatieoverzichten.',
                    'Link',
                    '/seed-documents/medicatielijst-template.xlsx',
                    NOW(), NOW()
                )
                ON CONFLICT (id) DO NOTHING;
                """);

            // Insert documents for Valpreventie
            migrationBuilder.Sql($"""
                INSERT INTO documents (id, package_id, name, description, type, file_url, created_at, updated_at)
                VALUES
                (
                    '{ValpreventieDoc1Id}',
                    '{ValpreventiePackageId}',
                    'Valrisico Checklist',
                    'Checklist voor het inschatten van valrisico bij cliënten.',
                    'Pdf',
                    '/seed-documents/valpreventie-checklist.pdf',
                    NOW(), NOW()
                ),
                (
                    '{ValpreventieDoc2Id}',
                    '{ValpreventiePackageId}',
                    'Valpreventie Assessment',
                    'Stapsgewijs assessment protocol voor valpreventie.',
                    'Steps',
                    NULL,
                    NOW(), NOW()
                )
                ON CONFLICT (id) DO NOTHING;
                """);

            // Set steps_content for the Valpreventie Assessment document
            migrationBuilder.Sql($$"""
                UPDATE documents
                SET steps_content = '[
                    {"step": 1, "title": "Persoonlijke gegevens", "description": "Vul de naam, geboortedatum en adres van de cliënt in."},
                    {"step": 2, "title": "Valgeschiedenis", "description": "Documenteer eerdere valincidenten (frequentie, omstandigheden, letsel)."},
                    {"step": 3, "title": "Medicatie-inventarisatie", "description": "Noteer alle huidige medicatie, let op valrisico-verhogende middelen."},
                    {"step": 4, "title": "Mobiliteitstest", "description": "Voer de Timed Up and Go test uit en noteer de score."},
                    {"step": 5, "title": "Omgevingsinspectie", "description": "Loop de woning door met de omgevingschecklist."},
                    {"step": 6, "title": "Voedingstoestand", "description": "Beoordeel de voedingstoestand (BMI, eetpatroon, vocht)."},
                    {"step": 7, "title": "Visus en gehoor", "description": "Vraag naar laatst oogarts-/audioloogbezoek, adviseer controle indien nodig."},
                    {"step": 8, "title": "Actieplan opstellen", "description": "Stel samen met de cliënt een persoonlijk actieplan op."}
                ]'
                WHERE id = '{{ValpreventieDoc2Id}}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM documents WHERE id IN (
                    '{VoedselbankDoc1Id}', '{VoedselbankDoc2Id}',
                    '{MedicatieDoc1Id}', '{MedicatieDoc2Id}',
                    '{ValpreventieDoc1Id}', '{ValpreventieDoc2Id}'
                );
                """);

            migrationBuilder.Sql($"""
                DELETE FROM packages WHERE id IN ('{MedicatiePackageId}', '{ValpreventiePackageId}');
                """);

            migrationBuilder.Sql($"""
                UPDATE packages
                SET synonyms = ARRAY[]::text[],
                    tags = ARRAY['voedselbank', 'voedselpakket', 'armoede', 'boodschappen', 'sociaal minimum', 'aanvraag']
                WHERE id = '{VoedselbankPackageId}';
                """);
        }
    }
}
