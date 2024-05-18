﻿using SuperApp.AccesoDatos.Conexion;
using SuperApp.AccesoDatos.Interfaz;
using SupperApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperApp.AccesoDatos.Excepciones;
using System.Reflection.PortableExecutable;

namespace SuperApp.AccesoDatos.DAO
{
    internal class EspecialidadDAO : IEspecialidad
    {
        public async Task<Response> Create(Especialidad data)
        {
            return await ExecuteNonQueryAsync("SP_C_ESPECIALIDAD", cmd =>
            {
                cmd.Parameters.AddWithValue("@nombreEspecialidad", data.NombreEspecialidad);
                cmd.Parameters.AddWithValue("@estado", data.IsActivo);
            },
            result =>
            {
                return result switch
                {
                    1 => new Response { Status = "Success", Message = "Registro Eliminado" },
                    -1 => new Response { Status = "Error", Message = "Error. No se encontro una especialidad con el ID proporcionado." },
                    -2 => new Response { Status = "Error", Message = "Error al eliminar especialidad" },
                    _ => new Response { Status = "Error", Message = "Código de retorno no reconocido." }
                };
            });
        }

        public async Task<Response> Delete(int id)
        {
            return await ExecuteNonQueryAsync("SP_D_ESPECIALIDAD", cmd =>
            {
                cmd.Parameters.AddWithValue("@idEspecialidad", id);
                var returnVlue = new SqlParameter
                {
                    ParameterName = "@returnValue",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue,
                };
                cmd.Parameters.Add(returnVlue);
            },
            result =>
            {
                return result switch
                {
                    1 => new Response { Status = "Success", Message = "Registro Eliminado" },
                    -1 => new Response { Status = "Error", Message = "Error. No se encontro una especialidad con el ID proporcionado." },
                    -2 => new Response { Status = "Error", Message = "Error al eliminar especialidad" },
                    _ => new Response  { Status = "Error", Message = "Código de retorno no reconocido."}
                };
            });
        }

        public async Task<Response<Especialidad>> Find(int id)
        {
            return await ExecuteReaderAsync("SP_F_ESPECIALIDAD", cmd =>
            {
                cmd.Parameters.AddWithValue("@idEspecialidad",id);
            }, reader =>
            {
                if( reader.Read())
                {
                    return new Especialidad()
                    {
                        IDEspecialidad = reader.GetInt32(reader.GetOrdinal("idEspecialidad")),
                        NombreEspecialidad = reader.GetString(reader.GetOrdinal("nombreEspecialidad"))
                    };
                }
                throw new EspecialidadNoEncontradaException("No se pudo encontrar la especialidad");
            });
        }

        public async Task<Response<IEnumerable<Especialidad>>> GetAll()
        {
            return await ExecuteReaderAsync("SP_R_ESPECIALIDAD", null, reader =>
            {
                var list = new List<Especialidad>();
                while (reader.Read())
                {
                    list.Add(new Especialidad
                    {
                        IDEspecialidad = reader.GetInt32(reader.GetOrdinal("idEspecialidad")),
                        NombreEspecialidad = reader.GetString(reader.GetOrdinal("nombreEspecialidad"))
                    });
                }
                return list.AsEnumerable();
            });
        }

        public async Task<Response> Update(Especialidad data)
        {
            return await ExecuteNonQueryAsync("SP_U_ESPECIALIDAD", cmd =>
            {
                cmd.Parameters.AddWithValue("@idEspecialidad", data.IDEspecialidad);
                cmd.Parameters.AddWithValue("@nombreEspecialidad", data.NombreEspecialidad);
                cmd.Parameters.AddWithValue("@estado", data.IsActivo);
            });
        }

        private async Task<Response> ExecuteNonQueryAsync(string storedProcedure,Action<SqlCommand> action,Func<int,Response> handleReturnValue=null)
        {
            var response = new Response();
            try
            {
                await CadenaConexion.Abrir();
                using var cmd = new SqlCommand(storedProcedure, CadenaConexion.conectar) { CommandType = CommandType.StoredProcedure };
                action?.Invoke(cmd);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                response.Status = "success";
                response.Message = "Operacion Realizada con Exito. ";
            }
            catch(SqlException ex)
            {
                response.Status="Error"; 
                response.Message = ex.Message;
            }
            finally
            {
                await CadenaConexion.Cerrar();
            }
            return response;
        }

        private async Task<Response<TEntity>> ExecuteReaderAsync<TEntity>(string storedProcedure,Action<SqlCommand> action, Func<SqlDataReader,TEntity> read)
        {
            var response=new Response<TEntity>();
            try
            {
                await CadenaConexion.Abrir();
                using var cmd = new SqlCommand(storedProcedure, CadenaConexion.conectar) { CommandType= CommandType.StoredProcedure };
                action?.Invoke(cmd);
                using var reader=await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                response.Data=read(reader);
                response.Status = "success";
                response.Message = "Operacion realizada con exito";
            }catch(EspecialidadNoEncontradaException ex)
            {
                response.Status="Error";
                response.Message=ex.Message;
            }catch(SqlException ex)
            {
                response.Status="Error";
                response.Message=ex.Message;
            }
            finally
            {
                await CadenaConexion.Cerrar();
            }
            return response;
        }

    }
}
