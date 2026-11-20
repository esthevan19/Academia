# Publicação segura do GymFlow na MonsterASP

## Segurança antes de publicar
A configuração pública `GymManagementPL/appsettings.json` não contém mais credenciais de banco.
Como uma versão anterior tinha senha embutida, **troque essa senha do SQL Server** no painel da MonsterASP. Remover do GitHub não apaga commits antigos.

## Configuração obrigatória na hospedagem
No painel MonsterASP do site `gymflowfit.runasp.net`, configure as variáveis de ambiente **no site**, não em código:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection` = string completa de conexão SQL Server, com a **nova** senha, obtida no painel do banco
- `Jwt__Key` = chave longa e secreta (já configurada anteriormente)
- `Database__ApplyMigrations=false` = NÃO aplicar migrações automaticamente

Evite publicar ou compartilhar tokens, senhas ou o perfil WebDeploy com credenciais. Após alterar variáveis, reinicie a aplicação no painel.

## Publicação do site
1. Garanta backup do banco SQL antes da publicação (e teste uma restauração).
2. Baixe o artefato `GymFlow-MVC-MonsterASP` de **MVC MonsterASP publish package** no GitHub Actions. Ele contém um ZIP interno com o resultado de `dotnet publish`.
3. Extraia o ZIP interno e publique os arquivos na pasta da aplicação pelo método oficial do site MonsterASP (Visual Studio WebDeploy/FTP/gerenciador de arquivos). Não envie o ZIP sem extrair se o painel não tiver opção de extração.
4. Preserve a configuração do IIS/painel existente e publique com o aplicativo interrompido se o painel exigir. Não apague arquivos nem o banco existentes sem backup.
5. Reinicie o site e abra `https://gymflowfit.runasp.net/Account/Login`.
6. Rode `scripts/Validar-Acesso-GymFlow.ps1` no PowerShell para verificar a autenticação e as permissões, usando uma conta já existente. O script faz **somente GET** após login; não cria ou altera cadastros.

## Banco de dados
A PR de migração de renovação de matrículas (#35) NÃO deve ser aplicada automaticamente.
A divergência de snapshot/migrações (#38) deve ser resolvida antes de novas migrações. Execute `Migrate()` somente depois de homologação em cópia e backup verificado.

## APK
Baixe o artefato `GymFlow-Android-APK` do workflow `Build Android APK` no GitHub.
Extraia `GymFlow.apk`, instale no Android e teste com a **mesma conta existente** que usa no MVC.

Verifique nos dois ambientes se alunos, planos, matrículas, aulas e treinos correspondem aos mesmos dados. Faça gravações de teste somente com autorização e backup do banco de produção; prefira uma base de homologação.

**Importante:** sem acesso autorizado ao painel de hospedagem e sem credenciais de teste, o CI valida permissões, compilação, banco descartável e acesso público, mas não confirma gravações de ponta a ponta na MonsterASP.
