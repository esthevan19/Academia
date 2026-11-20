#requires -Version 5.1
<#
  Teste autenticado e SOMENTE LEITURA do MVC/API publicado.
  Nenhum dado é criado, editado ou excluído.
  Exemplo: .\scripts\Validar-Acesso-GymFlow.ps1
#>
param(
    [string] $BaseUrl = 'https://gymflowfit.runasp.net'
)
$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
if (-not $BaseUrl.StartsWith('https://')) {
    throw 'Use somente HTTPS para enviar credenciais de autenticação.'
}
$cred = Get-Credential -Message 'Informe uma conta existente do GymFlow para testar a API'
if ($null -eq $cred) { throw 'Credenciais não informadas.' }
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($cred.Password)
try {
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $request = @{ email = $cred.UserName; password = $plainPassword } | ConvertTo-Json -Compress
    $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body $request -TimeoutSec 45
} finally {
    $plainPassword = $null
    $request = $null
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}
if ($null -eq $login -or $login.success -ne $true -or [string]::IsNullOrWhiteSpace($login.token)) {
    throw 'A API não retornou uma sessão válida.'
}
$roles = @($login.user.roles)
$priority = @('SuperAdmin','Admin','Gerente','Recepcionista','Instrutor','Aluno')
$role = $null
foreach ($candidate in $priority) {
    if ($roles -contains $candidate) { $role = $candidate; break }
}
if (-not $role) { throw 'Nenhum perfil conhecido foi retornado pela API.' }
Write-Host "Login OK - perfil $role. Testando permissões de leitura..." -ForegroundColor Green

$staff = @('SuperAdmin','Admin','Gerente','Recepcionista')
$managers = @('SuperAdmin','Admin','Gerente')
$workoutReaders = @('SuperAdmin','Admin','Gerente','Instrutor','Aluno')
$routes = @(
    @{ Path = '/api/members'; Allowed = $staff },
    @{ Path = '/api/memberships'; Allowed = $staff },
    @{ Path = '/api/plans'; Allowed = $managers },
    @{ Path = '/api/sessions'; Allowed = $priority },
    @{ Path = '/api/workouts'; Allowed = $workoutReaders; UnlinkedStudent = $true },
    @{ Path = '/api/admins'; Allowed = @('SuperAdmin') },
    @{ Path = '/api/me'; Allowed = @('Aluno'); UnlinkedStudent = $true }
)
$failures = 0
foreach ($route in $routes) {
    $expected = if ($route.Allowed -contains $role) { @(200) } else { @(403) }
    # Para Aluno sem vínculo, a API devolve conflito 409 em vez de dados próprios.
    if ($role -eq 'Aluno' -and $route.UnlinkedStudent) { $expected = @(200,409) }
    $status = 0
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl$($route.Path)" -Headers @{ Authorization = "Bearer $($login.token)"; Accept='application/json' } -UseBasicParsing -TimeoutSec 45
        $status = [int]$response.StatusCode
    } catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        } else {
            Write-Warning "$($route.Path): problema de rede ou certificado (sem resposta HTTP)"
        }
    }
    $passed = $expected -contains $status
    if (-not $passed) { $failures++ }
    $label = if ($passed) { 'OK' } else { 'FALHA' }
    Write-Host "$label $($route.Path) - HTTP $status (esperado: $($expected -join '/'))"
}
$login = $null
if ($failures -gt 0) {
    throw "$failures rotas falharam. Não publique outro APK até conferir backend, vínculo e perfis."
}
Write-Host 'Autenticação e permissões básicas passaram. Não foram realizados testes de gravação.' -ForegroundColor Green
