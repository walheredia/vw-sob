using System.Collections.Generic;

namespace vwsob
{
    /// <summary>
    /// Esta clase es utilizada para deserializar archivos JSON recuperados de la agenda de SOB
    /// </summary>
    class RegistroAgendamiento
    {
        public class Telefones
        {
            public int Tipo_Tel { get; set; }
            public int DDD { get; set; }
            public int Telefone { get; set; }
        }

        public class Servicio
        {
            public int TipoServicio { get; set; }
            public string Descripcion { get; set; }
            public string Observacion { get; set; }
        }

        public class Veiculo
        {
            public string Chassi { get; set; }
            public string Marca { get; set; }
            public string Modelo { get; set; }
            public string DescricaoModelo { get; set; }
            public string AnoModelo { get; set; }
            public string CorExterna { get; set; }
            public string CorInterna { get; set; }
            public string Placa { get; set; }
        }

        public class Agendamento
        {
            public int IdAgendamento { get; set; }
            public int TipoAgendamento { get; set; }
            public string ReparoRepetitivo { get; set; }
            public int IdStatus { get; set; }
            public string DataAgendamentoInicio { get; set; }
            public string DataAgendamentoFim { get; set; }
            public int MinutosExecucao { get; set; }
            public string UsuarioCriacao { get; set; }
            public string DtCriacao { get; set; }
            public string UsuarioAlteracao { get; set; }
            public Veiculo Veiculo { get; set; }
            public List<Servicio> Servicios { get; set; }
        }

        public class Registro
        {
            public int Origem_Id { get; set; }
            public int Sub_Origem_Id { get; set; }
            public string DN { get; set; }
            public string CpfCnpj { get; set; }
            public string Nome { get; set; }
            public string Email { get; set; }
            public List<Telefones> Telefones { get; set; }
            public string AutorizaDados { get; set; }
            public Agendamento Agendamento { get; set; }
            public int Preferencia_Contato { get; set; }
        }

    }
}
