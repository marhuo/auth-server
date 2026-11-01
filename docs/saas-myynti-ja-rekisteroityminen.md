# SaaS-sovelluksen myynti netissä: rekisteröityminen, roolit ja kutsut

*Muistiinpanot suunnittelukeskustelusta 19.11.2026. Mitään ei ole vielä toteutettu.*

Nämä muistiinpanot korvaavat aiemman yhteenvedon ne kohdat, jotka ovat ristiriidassa tämän kanssa. Erityisesti `tenant_admin`-rooli, domainit ja `JoinPolicy` hylättiin (ks. "Hylätyt ajatukset").

---

## 1. Perusperiaate

1. **Autentikointipalvelin** vastaa kysymykseen: *kuka olet ja mihin tenantteihin kuulut?*
2. **Tuote** (resurssipalvelin, esim. abc) vastaa kysymykseen: *onko tällä tenantilla maksettu tilaus, ja mitä roolit tarkoittavat?*
3. **Stripe** ottaa rahat ja ilmoittaa tuotteelle webhookilla, kun maksu on tehty.

**Autentikointipalvelimelle kaikki tuotteiden roolit ovat samanarvoisia.** Vain tuote antaa niille merkityksen. Autentikointipalvelin ymmärtää vain omat roolinsa (`auth_server_admin`, `tenant_id = null`), jotka ovat palvelimen oman ylläpidon rooleja.

Auth0, Okta ja Atlassian toimivat samoin. Niillä on erityisrooleja vain oman palvelunsa hallintaan, ei asiakkaan sovellusten rooleille.

## 2. Tarvittavat sovellukset

Alussa riittää kaksi:

| Sovellus | Tekee |
|---|---|
| Autentikointipalvelin | Tunnuksen luonti, kirjautuminen, tenantit ja roolit tuotteen pyynnöstä |
| Tuote (abc) | Itse sovellus, työtilan luonti, kutsut, laskutussivu, Stripe |

Kotisivu (markkinointi) ja Stripe (ulkoinen palvelu) tarvitaan myös, mutta ne eivät ole omia sovelluksia.

**Yhteinen hallintaportaali** on vasta myöhempi vaihe, kun tuotteita on useita (vrt. `admin.atlassian.com`). Silloin työtilan luonti, kutsut ja laskutus siirretään tuotteesta portaaliin. Portaali on yksi resurssipalvelin muiden joukossa, ja sillä on omat roolinsa. Autentikointipalvelimeen ei tarvitse koskea.

Vertailu Atlassianiin:

| Atlassian | Tämä järjestelmä |
|---|---|
| `id.atlassian.com` (vain identiteetti) | Autentikointipalvelin |
| `admin.atlassian.com` (organisaatiot, kutsut, laskutus) | Alussa tuote itse, myöhemmin oma hallintaportaali |
| Jira, Confluence | abc, xyz |

## 3. Rekisteröityminen jaetaan kahteen osaan

| Vaihe | Mitä | Missä | Miksi |
|---|---|---|---|
| 1. Tunnuksen luonti | Sähköposti, vahvistuskoodi, salasana | Autentikointipalvelin | Identiteetti. Tuote ei koskaan käsittele salasanoja. |
| 2. Työtilan luonti | Yrityksen nimi, tenant, admin-rooli | Tuote | Vain tuote tietää, mikä sen rooleista on admin. |

### Kulku esimerkin kautta

1. **Liisa painaa kotisivulla "Kokeile".** Linkki vie abc:hen, ja abc ohjaa autentikointipalvelimelle lisätiedolla `prompt=create` (näytä rekisteröitymissivu kirjautumisen sijaan).
2. **Liisa luo tunnuksen autentikointipalvelimella:** sähköposti, koodi sähköpostista ja salasana. Autentikointipalvelin luo **pelkän käyttäjän**, ei tenanttia eikä rooleja.
3. **Liisa palaa abc:hen kirjautuneena.** abc näkee, ettei Liisa kuulu yhteenkään tenanttiin, ja kysyy yrityksen tai tiimin nimen.
4. **Liisa kirjoittaa "Liisan Leipomo".** abc:n palvelin kutsuu nykyistä `POST api/tenant` -rajapintaa (`Name`, `AdminEmail`, `AdminRoleNames = [abc_admin]`). abc kertoo itse, mikä sen rooleista on admin.
5. **Liisa on omassa työtilassaan.** Kirjautumistiedoissa ei vielä ole uutta tenanttia, joten abc ohjaa Liisan uudelleen autentikointipalvelimen kautta. Salasanaa ei kysytä, koska Liisa on jo kirjautunut.

Jos Liisalla on jo tunnus, vaihe 2 on pelkkä kirjautuminen, ja hän voi luoda uuden työtilan.

### Miksi näin

- Mitään ei luoda ennen kuin sähköposti on vahvistettu. Kukaan ei voi luoda käyttäjää toisen sähköpostilla.
- Tenanttien määrää ei ole rajattu, joten kukaan ei voi estää toista luomasta omaa työtilaansa.
- Jokaisen uuden tuotteen rekisteröinti toimii samalla tavalla ilman lisätyötä autentikointipalvelimeen.

### Vaihtoehto: koko rekisteröinti tuotteessa

Toimisi jo nykyisellä koodilla: abc:n lomake (sähköposti + yrityksen nimi) → `POST api/tenant` → käyttäjä asettaa salasanan kutsulinkillä.

Ei suositella, koska:
- kuka tahansa voi luoda tenantin toisen sähköpostilla ennen vahvistusta
- olemassa oleva käyttäjä saa roolit hiljaa ilman sähköpostia (nykyinen kutsun toiminta)
- jokainen tuote joutuu suojaamaan lomakkeensa roskarekisteröinneiltä itse

Kelpaa kuitenkin demoon tai kokeiluun, koska muutoksia ei tarvita.

## 4. Tuotteen ylläpitäjät, jotka eivät kuulu asiakkaan tenanttiin

Esimerkiksi abc:n tukihenkilöt. **Uutta roolityyppiä ei tarvita**, eikä `tenant_id = null` -säännön merkitys muutu.

Oma yritys on yksi tenant muiden joukossa. Tukihenkilöt ovat sen jäseniä tuoteroolilla (esim. `abc_support`), ja abc päättää, mitä rooli saa tehdä.

## 5. Kutsut: nykyinen ongelma ja ratkaisu

**Nykytila** (`UserService.InviteAsync`): kutsu ei ole erillinen tieto, vaan se lisää roolit heti. Sähköposti (salasanalinkki) lähtee vain, jos käyttäjällä ei ole salasanaa. Siksi:
- kutsua ei voi perua eikä korjata, koska ei ole mitään odottavaa asiaa
- olemassa oleva käyttäjä ei saa sähköpostia, vaan saa vain uudet roolit

**Ratkaisu** (sama kuin Auth0:n Organization invitations): kutsu omaksi tiedokseen.

- Tiedot: sähköposti, tenant, roolit, tila (odottaa, hyväksytty, peruttu), vanhenemisaika
- Roolit annetaan vasta, kun kutsu hyväksytään
- Kutsun voi perua ja lähettää uudelleen, ja väärät roolit voi korjata ennen hyväksyntää
- Olemassa olevalle käyttäjälle lähtee sähköposti "liity tenanttiin X"

## 6. Osto (ennallaan aiemmasta yhteenvedosta)

1. Käyttäjä avaa abc:ssä Laskutus-sivun. Osta-napin näkee vain se, jolla on ostovaltuus, ja sen päättää abc omien rooliensa perusteella.
2. abc:n palvelin luo Stripe Checkout Sessionin ja lisää mukaan `metadata: { tenant_id, app }`.
3. Käyttäjä maksaa Stripen sivulla. Yrityksen nimi ja ALV-tunnus kysytään tässä.
4. Stripe lähettää webhookin. abc tallentaa omaan kantaansa `Subscription(TenantId, Status)`.
5. Kuukausiveloitus, peruutus ja korttimuutokset hoituvat Stripen kautta (webhookit, asiakasportaali).

Autentikointipalvelin ei tiedä maksuista mitään.

## 7. Hylätyt ajatukset

| Ajatus | Miksi hylättiin |
|---|---|
| `tenant_admin`-rooli autentikointipalvelimelle | Rikkoisi periaatteen. Tuote kertoo admin-roolin itse `POST api/tenant` -kutsussa. |
| `resource_server_admin`-rooli (`tenant_id = null`) | Tarpeeton. Oma yritys on tenant, ja tukihenkilöt saavat sen sisällä tuoteroolin. |
| Domainit, `TenantDomain`, `JoinPolicy`, `JoinRequest` | Lisäominaisuus isoille yrityksille, ei tarvita myyntiin. Voidaan lisätä myöhemmin rikkomatta mitään. Kutsut riittävät. |
| Tenantin luonti autentikointipalvelimen rekisteröitymisessä | Autentikointipalvelimen pitäisi tietää, mikä rooli on admin. |
| Osto ennen rekisteröitymistä | Ostajaa ei voi liittää luotettavasti oikeaan tenanttiin. |

## 8. Tehtävät autentikointipalvelimeen (kun aikaa on)

- [ ] Kutsu omaksi tiedokseen: peruminen, uudelleenlähetys, sähköposti myös olemassa olevalle käyttäjälle
- [ ] Rekisteröitymissivu: `prompt=create`, sähköpostin vahvistuskoodi ennen käyttäjän luontia, salasana
- [ ] Kirjautuminen käyttäjälle, jolla ei vielä ole yhtään tenanttia (tuotteen onboarding tarvitsee tämän)
- [ ] Kirjautumistietojen päivitys, kun tuote on juuri luonut uuden tenantin (vaihe 5)

## 9. Avoimet kysymykset

- Ostovaltuus: riittääkö tuotteen admin-rooli (`abc_admin`), vai tarvitaanko erillinen tuoterooli?
- Kokeilujakso: ilman korttia (oma logiikka) vai Stripen trial kortin kanssa?
- Stripe vai Paddle (ALV-vastuu)?
- Kirjautumisen osoite: aliverkkotunnus (`login.yritys.fi`) vai sama domain reitittimellä?
