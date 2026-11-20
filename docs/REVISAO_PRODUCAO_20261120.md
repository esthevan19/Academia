# Revisão de produção: GymFlow / MonsterASP

## Evidências do log fornecido em 20/11/2026

- A aplicação inicia em modo `Production` e responde a consultas reais SQL/Identity.
- O log apresenta **Data Protection com chave efêmera** (não persistente) e **aviso de licença AutoMapper**.
- **Não há uma exceção do GET /Member/Create** no trecho compartilhado. Não é possível atribuir o HTTP 500 apenas a essas mensagens.
- Alterações neste documento não são uma migração do banco: mantenha `Database__ApplyMigrations=false`.

## Chaves persistentes para cookies e antiforgery

A configuração `DataProtection__KeysDirectory` agora permite guardar chaves em pasta privada persistente (fora do diretório servido ao público e fora da publicação WebDeploy). Um administrador do painel deve escolher **um caminho absoluto com permissão de escrita para o processo do aplicativo**, por exemplo uma pasta privada administrada pelo provedor. Não copie o exemplo de caminho sem confirmar a existência e ACL.

Em Windows, os arquivos de chaves serão protegidos por DPAPI no escopo da máquina. Isso **não substitui as permissões de acesso do diretório**. A pasta precisa ser retida entre publicações, e, se houver mais de um servidor, todas as instâncias devem compartilhar um mecanismo de proteção compatível.

**Não publique chaves XML, senhas SQL, tokens ou capturas desse diretório.** Se a pasta ou permissão não for suportada na hospedagem, não configure a variável: a aplicação manterá o mecanismo padrão do ASP.NET Core, que pode ser efêmero. Neste caso, solicite ao suporte da hospedagem um repositório persistente de Data Protection.

## Aviso AutoMapper

O log mostra que AutoMapper 15.1.4 **não recebeu uma chave de licença**. A versão 15+ usa licença comercial ou licença recíproca com obrigações específicas. **Não foi feito downgrade para 13/14**, pois essas versões têm vulnerabilidade alta publicada. Para uso comercial que exigir licença, configure `AUTOMAPPER_LICENSE_KEY` no painel, conforme sua licença obtida oficialmente; não grave a chave em `appsettings.json` nem no Git. Este aviso, isoladamente, não prova a causa do HTTP 500.

## Publicação

1. Faça backup verificado do SQL Server antes de publicar.
2. Confirme `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Database__ApplyMigrations=false`.
3. Atualize o branch `main` no seu computador e publique `GymManagementPL` com o perfil WebDeploy existente.
4. Se a publicação for feita pelo ZIP de artefato GitHub Actions, extraia o pacote **interno** para a pasta da aplicação: não envie o ZIP de artefato bruto ao servidor sem extração.
5. Abra `https://gymflowfit.runasp.net/api/health`. Na versão nova deve responder HTTP 200 com `status: ok` e `revision: 20261120-review-1`. HTTP 404 significa que a versão nova pode não estar publicada; HTTP 503 indica falha de conexão ao banco (não compartilhe a string SQL).
6. Se o HTTP 500 persistir, use o **código de referência** exibido na página atualizada e verifique o log de erro correspondente. Envie só a exceção sem credenciais nem dados de alunos.
7. Faça login SuperAdmin, abra Alunos > Cadastrar aluno, faça um teste autorizado, retorne à listagem e verifique a persistência. Só então teste os demais perfis e o APK.

## Estado dos testes

A suíte `MVC authenticated member create integration` abre e posta um formulário real com antiforgery e persiste em SQL descartável, além de validar o GET e acesso autorizado/negado das páginas dos seis perfis. Os testes em CI **não executam gravação na MonsterASP** nem cobrem um Android físico.

A migração de renovação de matrícula continua pendente (#35, #38) e **não deve ser aplicada em produção** sem alinhamento das migrações e backup.
