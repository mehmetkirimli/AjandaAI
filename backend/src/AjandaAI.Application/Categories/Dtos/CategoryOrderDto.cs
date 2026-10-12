// PUT /api/categories/order isteğinin girdi modelidir (ADR 0023 Karar 2).
// Mevcut TÜM kategorilerin (aktif + pasif) Id'leri istenen sırayla verilir.

namespace AjandaAI.Application.Categories.Dtos;

public record CategoryOrderDto(int[]? CategoryIds);
